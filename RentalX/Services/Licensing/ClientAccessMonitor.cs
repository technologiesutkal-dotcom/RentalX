using System.Net.Http;
using System.Net.Http.Headers;
using System.Windows;
using System.Windows.Threading;
using RentalX.Admin.Services;
using RentalX.Services;

namespace RentalX.Services.Licensing;

public sealed class ClientAccessMonitor : IClientAccessMonitor, IDisposable
{
    private static readonly TimeSpan Interval = TimeSpan.FromSeconds(90);

    private readonly ILicenseApiClient _api;
    private readonly IPlatformSession _platform;
    private readonly ISessionService _session;
    private readonly IMachineIdService _machine;
    private readonly HttpClient _http;
    private DispatcherTimer? _timer;
    private bool _forcingSignOut;
    private int _offlineStrikes;

    public ClientAccessMonitor(
        ILicenseApiClient api,
        IPlatformSession platform,
        ISessionService session,
        IMachineIdService machine,
        HttpClient http)
    {
        _api = api;
        _platform = platform;
        _session = session;
        _machine = machine;
        _http = http;
    }

    public void Start()
    {
        if (!_platform.IsClient)
        {
            return;
        }

        _forcingSignOut = false;
        _offlineStrikes = 0;

        var dispatcher = Application.Current?.Dispatcher;
        if (dispatcher is null)
        {
            return;
        }

        dispatcher.Invoke(() =>
        {
            _timer ??= new DispatcherTimer { Interval = Interval };
            _timer.Tick -= OnTick;
            _timer.Tick += OnTick;
            if (!_timer.IsEnabled)
            {
                _timer.Start();
            }
        });

        _ = EnsureAccessAsync();
    }

    public void Stop()
    {
        var dispatcher = Application.Current?.Dispatcher;
        if (dispatcher is null)
        {
            return;
        }

        dispatcher.Invoke(() =>
        {
            if (_timer is null)
            {
                return;
            }

            _timer.Stop();
            _timer.Tick -= OnTick;
        });
    }

    public async Task<bool> EnsureAccessAsync(CancellationToken ct = default)
    {
        if (!_platform.IsClient || _forcingSignOut)
        {
            return true;
        }

        try
        {
            // 1) Token/session still valid + client Active
            if (!string.IsNullOrWhiteSpace(_platform.Token))
            {
                using var request = new HttpRequestMessage(HttpMethod.Get, BuildUri("api/auth/access"));
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _platform.Token);
                using var response = await _http.SendAsync(request, ct);
                if (response.StatusCode == System.Net.HttpStatusCode.Forbidden)
                {
                    await ForceSignOutAsync("Your account was deactivated by Utkal Technologies.\n\nYou will be signed out.");
                    return false;
                }

                if (response.IsSuccessStatusCode)
                {
                    _offlineStrikes = 0;
                }
            }

            // 2) Machine serial still Approved
            var serial = _machine.GetMachineId();
            var (data, error) = await _api.GetSerialStatusAsync(serial, ct);
            if (!string.IsNullOrWhiteSpace(error) && error.Contains("Cannot reach", StringComparison.OrdinalIgnoreCase))
            {
                _offlineStrikes++;
                // Allow brief offline; lock after repeated failures (~7.5 min at 90s interval)
                if (_offlineStrikes >= 5)
                {
                    await ForceSignOutAsync("Cannot verify license with the server.\n\nYou will be signed out. Try again when online.");
                    return false;
                }

                return true;
            }

            _offlineStrikes = 0;
            var status = data?.Data?.Status;
            if (string.Equals(status, "Deactivated", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(status, "Revoked", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(status, "Rejected", StringComparison.OrdinalIgnoreCase))
            {
                await ForceSignOutAsync(
                    "Access for this computer was deactivated by Utkal Technologies.\n\nYou will be signed out.");
                return false;
            }

            return true;
        }
        catch
        {
            // Network blip — ignore until next tick
            return true;
        }
    }

    private void OnTick(object? sender, EventArgs e) => _ = EnsureAccessAsync();

    private async Task ForceSignOutAsync(string message)
    {
        if (_forcingSignOut)
        {
            return;
        }

        _forcingSignOut = true;
        Stop();

        await Application.Current.Dispatcher.InvokeAsync(() =>
        {
            MessageBox.Show(message, "RentalX Access", MessageBoxButton.OK, MessageBoxImage.Warning);
            _platform.Clear();
            _session.SignOut();
        });
    }

    private Uri BuildUri(string path)
    {
        var baseUrl = string.IsNullOrWhiteSpace(_api.BaseUrl) ? LicenseApiDefaults.BaseUrl : _api.BaseUrl.TrimEnd('/');
        return new Uri($"{baseUrl}/{path.TrimStart('/')}");
    }

    public void Dispose() => Stop();
}
