using System.Windows.Media;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RentalX.Admin.Models;
using RentalX.Admin.Services;
using RentalX.Data.Repositories;
using RentalX.Models;
using RentalX.Services;
using RentalX.Services.Licensing;

namespace RentalX.ViewModels;

public partial class LoginViewModel : ObservableObject
{
    private static readonly string[] WelcomeTips =
    [
        "Book → Dispatch → Return — run the full rental cycle in one place.",
        "Track overdue returns and balances from the Dashboard at a glance.",
        "Set daily rates in Inventory; bookings calculate amounts automatically.",
        "Print invoices after settle — PDFs save under Documents/RentalX.",
        "Keep Business Settings up to date for branded reports and invoices.",
        "Use Apply for Access once per PC if this is a new installation."
    ];

    private readonly ILicenseApiClient _api;
    private readonly IAdminSession _adminSession;
    private readonly IPlatformSession _platform;
    private readonly ISessionService _session;
    private readonly IMachineIdService _machine;
    private readonly ISettingsRepository _settings;
    private readonly DispatcherTimer _tipTimer;
    private readonly string _machineSerial;
    private int _tipIndex;

    public LoginViewModel(
        ILicenseApiClient api,
        IAdminSession adminSession,
        IPlatformSession platform,
        ISessionService session,
        IMachineIdService machine,
        ISettingsRepository settings)
    {
        _api = api;
        _adminSession = adminSession;
        _platform = platform;
        _session = session;
        _machine = machine;
        _settings = settings;
        ApiBaseUrl = LicenseApiDefaults.BaseUrl;
        _machineSerial = _machine.GetMachineId();
        MachineName = _machine.GetMachineName();
        DefaultHint = "Need a login or activation? Contact Utkal Technologies.";
        RefreshWelcome();
        _tipIndex = Math.Abs(Environment.TickCount) % WelcomeTips.Length;
        WelcomeTip = WelcomeTips[_tipIndex];

        _tipTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(7) };
        _tipTimer.Tick += (_, _) =>
        {
            RefreshWelcome();
            _tipIndex = (_tipIndex + 1) % WelcomeTips.Length;
            WelcomeTip = WelcomeTips[_tipIndex];
        };
        _tipTimer.Start();
    }

    public event Action? LoginSucceeded;

    [ObservableProperty] private string _companyDisplay = "RentalX Platform";
    [ObservableProperty] private string _apiBaseUrl = LicenseApiDefaults.BaseUrl;
    [ObservableProperty] private string _username = string.Empty;
    [ObservableProperty] private string _password = string.Empty;
    [ObservableProperty] private string _machineName = string.Empty;
    [ObservableProperty] private string _welcomeGreeting = "Welcome";
    [ObservableProperty] private string _welcomeDate = string.Empty;
    [ObservableProperty] private string _welcomeTip = string.Empty;
    [ObservableProperty] private string _errorMessage = string.Empty;
    [ObservableProperty] private string _infoMessage = string.Empty;
    [ObservableProperty] private string _defaultHint = string.Empty;
    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private bool _showApplyForm;
    [ObservableProperty] private bool _showPassword;
    [ObservableProperty] private Brush _errorBrush = Brushes.Transparent;

    [ObservableProperty] private string _applyCompany = string.Empty;
    [ObservableProperty] private string _applyEmail = string.Empty;
    [ObservableProperty] private string _applyPhone = string.Empty;
    [ObservableProperty] private string _applyAddress = string.Empty;
    [ObservableProperty] private string _applyPincode = string.Empty;
    [ObservableProperty] private string _applyNotes = string.Empty;

    [RelayCommand]
    private async Task ToggleApply()
    {
        ErrorMessage = string.Empty;
        InfoMessage = string.Empty;

        try
        {
            ApplyApiBaseUrl();
            var (status, _) = await _api.GetSerialStatusAsync(_machineSerial);
            if (status?.Data is not null)
            {
                var already = status.Data.Status switch
                {
                    "Approved" => "This computer is already registered and activated. Sign in with your Login ID, or contact Utkal Technologies.",
                    "Pending" => "An access request for this computer is already pending. Contact Utkal Technologies for updates.",
                    "Rejected" => "A previous access request was not approved. Contact Utkal Technologies for help.",
                    "Deactivated" => "Access for this computer was deactivated. Contact Utkal Technologies to re-activate.",
                    "Revoked" => "Access for this computer was revoked. Contact Utkal Technologies.",
                    _ => "This computer is already registered. Only one application per PC is allowed. Contact Utkal Technologies."
                };
                SetError(already);
                ShowApplyForm = false;
                return;
            }
        }
        catch
        {
            // If API down, still allow form; Apply will fail with clear error.
        }

        ShowApplyForm = !ShowApplyForm;
    }

    [RelayCommand]
    private async Task ApplyAsync()
    {
        ErrorMessage = string.Empty;
        InfoMessage = string.Empty;

        if (string.IsNullOrWhiteSpace(ApplyCompany) || ApplyCompany.Trim().Length < 2)
        {
            SetError("Company name is required.");
            return;
        }

        if (string.IsNullOrWhiteSpace(ApplyEmail) || !ApplyEmail.Contains('@'))
        {
            SetError("Valid email is required.");
            return;
        }

        if (string.IsNullOrWhiteSpace(ApplyPhone) || ApplyPhone.Trim().Length < 8)
        {
            SetError("Valid phone is required.");
            return;
        }

        if (string.IsNullOrWhiteSpace(ApplyAddress) || ApplyAddress.Trim().Length < 5)
        {
            SetError("Address is required.");
            return;
        }

        if (string.IsNullOrWhiteSpace(ApplyPincode) || ApplyPincode.Trim().Length < 4)
        {
            SetError("Pincode is required.");
            return;
        }

        IsBusy = true;
        try
        {
            ApplyApiBaseUrl();
            var (data, error) = await _api.ApplySerialAsync(new SerialApplyRequest
            {
                SerialNumber = _machineSerial,
                MachineName = MachineName,
                CompanyName = ApplyCompany.Trim(),
                Email = ApplyEmail.Trim(),
                Phone = ApplyPhone.Trim(),
                Address = ApplyAddress.Trim(),
                Pincode = ApplyPincode.Trim(),
                Notes = string.IsNullOrWhiteSpace(ApplyNotes) ? null : ApplyNotes.Trim()
            });

            if (data is null)
            {
                SetError(SanitizePublicMessage(error) ?? "Application failed.");
                return;
            }

            InfoMessage = SanitizePublicMessage(data.Message) ?? "Application submitted. Utkal Technologies will review it shortly.";
            ShowApplyForm = false;
        }
        catch (Exception ex)
        {
            SetError(ex.Message);
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task LoginAsync()
    {
        ErrorMessage = string.Empty;
        InfoMessage = string.Empty;

        if (string.IsNullOrWhiteSpace(Username) || string.IsNullOrWhiteSpace(Password))
        {
            SetError("Enter login ID and password.");
            return;
        }

        if (string.IsNullOrWhiteSpace(ApiBaseUrl))
        {
            SetError($"API URL is required (e.g. {LicenseApiDefaults.BaseUrl}).");
            return;
        }

        IsBusy = true;
        try
        {
            ApplyApiBaseUrl();
            var (data, error) = await _api.LoginAsync(new LoginRequest
            {
                Username = Username.Trim(),
                Password = Password,
                SerialNumber = _machineSerial
            });

            if (data is null)
            {
                SetError(SanitizePublicMessage(error) ?? "Login failed.");
                return;
            }

            var loginId = (data.Username ?? Username).Trim();
            _platform.Set(data.Token, data.Role, data.DisplayName, data.ClientId, data.CompanyName, loginId);

            if (string.Equals(data.Role, UserRoles.SuperAdmin, StringComparison.OrdinalIgnoreCase))
            {
                _adminSession.SetSession(data.Token, data.DisplayName, data.Role, loginId);
                CompanyDisplay = "Super Admin";
            }
            else if (string.Equals(data.Role, UserRoles.Client, StringComparison.OrdinalIgnoreCase))
            {
                _session.SignIn(new LoginAccount
                {
                    Id = data.ClientId ?? 0,
                    Username = loginId,
                    DisplayName = data.DisplayName,
                    MustChangePassword = false
                });
                CompanyDisplay = data.CompanyName ?? "Client";

                await _settings.SyncFromClientProfileAsync(
                    data.CompanyName,
                    data.ContactName ?? data.DisplayName ?? data.CompanyName,
                    data.Phone,
                    data.Email,
                    data.Address,
                    data.City);
            }
            else
            {
                SetError($"Unknown role: {data.Role}");
                return;
            }

            _tipTimer.Stop();
            LoginSucceeded?.Invoke();
        }
        catch (Exception ex)
        {
            SetError(ex.Message);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void RefreshWelcome()
    {
        var now = DateTime.Now;
        WelcomeGreeting = now.Hour switch
        {
            < 12 => "Good morning",
            < 17 => "Good afternoon",
            _ => "Good evening"
        };
        WelcomeDate = now.ToString("dddd, d MMMM yyyy · HH:mm");
    }

    private string? SanitizePublicMessage(string? message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return message;
        }

        var text = message;
        if (!string.IsNullOrWhiteSpace(_machineSerial) && text.Contains(_machineSerial, StringComparison.OrdinalIgnoreCase))
        {
            text = text.Replace(_machineSerial, "this computer", StringComparison.OrdinalIgnoreCase);
        }

        return text;
    }

    private void ApplyApiBaseUrl()
    {
        try
        {
            _api.BaseUrl = ApiBaseUrl.Trim();
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Invalid API URL: {ex.Message}", ex);
        }
    }

    private void SetError(string message)
    {
        ErrorMessage = message;
        ErrorBrush = new SolidColorBrush(Color.FromRgb(220, 38, 38));
    }
}
