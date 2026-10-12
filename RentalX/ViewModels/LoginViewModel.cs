using System.Windows;
using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RentalX.Admin.Models;
using RentalX.Admin.Services;
using RentalX.Data.Repositories;
using RentalX.Helpers;
using RentalX.Models;
using RentalX.Services;
using RentalX.Services.Licensing;

namespace RentalX.ViewModels;

public partial class LoginViewModel : ObservableObject
{
    private readonly ILicenseApiClient _api;
    private readonly IAdminSession _adminSession;
    private readonly IPlatformSession _platform;
    private readonly ISessionService _session;
    private readonly IMachineIdService _machine;
    private readonly ISettingsRepository _settings;

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
        ApiBaseUrl = "http://localhost:5159";
        SerialNumber = _machine.GetMachineId();
        DisplaySerial = SerialFormatter.Format(SerialNumber);
        MachineName = _machine.GetMachineName();
        DefaultHint =
            "Need a login or activation? Contact Utkal Technologies.";
        _ = RefreshSerialStatusAsync();
    }

    public event Action? LoginSucceeded;

    [ObservableProperty] private string _companyDisplay = "RentalX Platform";
    [ObservableProperty] private string _apiBaseUrl = "http://localhost:5159";
    [ObservableProperty] private string _username = string.Empty;
    [ObservableProperty] private string _password = string.Empty;
    [ObservableProperty] private string _serialNumber = string.Empty;
    [ObservableProperty] private string _displaySerial = string.Empty;
    [ObservableProperty] private string _machineName = string.Empty;
    [ObservableProperty] private string _serialStatusText = "Checking serial status…";
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

        // Block opening apply form if this laptop already has a registration.
        try
        {
            ApplyApiBaseUrl();
            var (status, _) = await _api.GetSerialStatusAsync(SerialNumber);
            if (status?.Data is not null)
            {
                var already = status.Data.Status switch
                {
                    "Approved" => "This laptop is already registered and activated. You cannot apply again. Sign in with your Login ID, or contact Utkal Technologies.",
                    "Pending" => "This laptop already applied. Your request is pending approval — you cannot apply again. Contact Utkal Technologies for status.",
                    "Rejected" => "This laptop already applied and was not approved. You cannot apply again. Contact Utkal Technologies.",
                    "Deactivated" => "This laptop was deactivated. You cannot apply again. Contact Utkal Technologies to re-activate.",
                    "Revoked" => "Access for this laptop was revoked. You cannot apply again. Contact Utkal Technologies.",
                    _ => "This laptop serial is already registered. Only one application per computer is allowed. Contact Utkal Technologies."
                };
                SetError(already);
                ShowApplyForm = false;
                SerialStatusText = already;
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
    private void CopySerial()
    {
        try
        {
            // Copy raw machine id (not truncated display) so Super Admin paste matches login.
            Clipboard.SetText(SerialNumber);
            InfoMessage = "Serial copied. Use Apply for Access or send it to Super Admin.";
        }
        catch (Exception ex)
        {
            SetError($"Could not copy serial: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task RefreshSerialStatusAsync()
    {
        try
        {
            ApplyApiBaseUrl();
            var (data, error) = await _api.GetSerialStatusAsync(SerialNumber);
            SerialStatusText = data?.Message ?? error ?? "Unable to check serial status.";
        }
        catch (Exception)
        {
            SerialStatusText = "Start License API to check serial status.";
        }
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
                SerialNumber = SerialNumber,
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
                SetError(error ?? "Application failed.");
                return;
            }

            InfoMessage = data.Message ?? "Application submitted.";
            SerialStatusText = data.Message ?? SerialStatusText;
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
            SetError("API URL is required (local: http://localhost:5159).");
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
                SerialNumber = SerialNumber
            });

            if (data is null)
            {
                SetError(string.IsNullOrWhiteSpace(error) ? "Login failed." : error);
                await RefreshSerialStatusAsync();
                return;
            }

            _platform.Set(data.Token, data.Role, data.DisplayName, data.ClientId, data.CompanyName);

            if (string.Equals(data.Role, UserRoles.SuperAdmin, StringComparison.OrdinalIgnoreCase))
            {
                _adminSession.SetSession(data.Token, data.DisplayName, data.Role, data.Username ?? Username.Trim());
                CompanyDisplay = "Super Admin";
            }
            else if (string.Equals(data.Role, UserRoles.Client, StringComparison.OrdinalIgnoreCase))
            {
                _session.SignIn(new LoginAccount
                {
                    Id = data.ClientId ?? 0,
                    Username = Username.Trim(),
                    DisplayName = data.DisplayName,
                    MustChangePassword = false
                });
                CompanyDisplay = data.CompanyName ?? "Client";

                // Prefill Business Settings from Apply / portal profile (blanks & defaults only).
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
