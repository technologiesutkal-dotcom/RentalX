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
using RentalX.ViewModels.Base;

namespace RentalX.ViewModels;

public partial class SettingsViewModel : PageViewModelBase
{
    private readonly ISettingsRepository _settings;
    private readonly ISessionService _session;
    private readonly IPlatformSession _platform;
    private readonly ILicenseApiClient _api;

    public SettingsViewModel(
        ISettingsRepository settings,
        ISessionService session,
        IPlatformSession platform,
        ILicenseApiClient api)
    {
        _settings = settings;
        _session = session;
        _platform = platform;
        _api = api;
    }

    public string[] BusinessTypes { get; } =
    [
        "General rental",
        "Event / Tent house",
        "Equipment rental",
        "Vehicle rental",
        "Costume / Fashion",
        "Camera / AV",
        "Construction tools",
        "Furniture rental",
        "Other"
    ];

    [ObservableProperty] private string _businessType = "General rental";
    [ObservableProperty] private string _companyName = "My Rental Business";
    [ObservableProperty] private string _ownerName = "Owner";
    [ObservableProperty] private string _address = string.Empty;
    [ObservableProperty] private string _city = string.Empty;
    [ObservableProperty] private string _phone = string.Empty;
    [ObservableProperty] private string _email = string.Empty;
    [ObservableProperty] private string _gstNumber = string.Empty;
    [ObservableProperty] private bool _showGstOnReports = true;
    [ObservableProperty] private string _currency = "INR (₹)";
    [ObservableProperty] private string _reportFooter = "Thank you for your business";
    [ObservableProperty] private bool _enableNotifications = true;
    [ObservableProperty] private bool _autoBackup = true;
    [ObservableProperty] private string _formMessage = string.Empty;
    [ObservableProperty] private Brush _formMessageBrush = Brushes.Transparent;
    [ObservableProperty] private string _companyError = string.Empty;
    [ObservableProperty] private string _ownerError = string.Empty;
    [ObservableProperty] private string _phoneError = string.Empty;
    [ObservableProperty] private string _emailError = string.Empty;

    [ObservableProperty] private string _currentLoginId = string.Empty;
    [ObservableProperty] private string _currentPassword = string.Empty;
    [ObservableProperty] private string _newPassword = string.Empty;
    [ObservableProperty] private string _confirmPassword = string.Empty;
    [ObservableProperty] private string _loginMessage = string.Empty;
    [ObservableProperty] private Brush _loginMessageBrush = Brushes.Transparent;
    [ObservableProperty] private bool _isChangingPassword;

    public override async Task LoadAsync()
    {
        var s = await _settings.GetAsync();
        BusinessType = string.IsNullOrWhiteSpace(s.BusinessType) ? "General rental" : s.BusinessType;
        if (!BusinessTypes.Contains(BusinessType))
        {
            BusinessType = "Other";
        }

        CompanyName = s.CompanyName;
        OwnerName = s.OwnerName;
        Address = s.Address;
        City = s.City;
        Phone = s.Phone;
        Email = s.Email;
        GstNumber = s.GstNumber;
        ShowGstOnReports = s.ShowGstOnReports;
        Currency = s.Currency;
        ReportFooter = s.ReportFooter;
        EnableNotifications = s.EnableNotifications;
        AutoBackup = s.AutoBackup;

        // Always show the signed-in Login ID (platform username), never a local default like "admin".
        CurrentLoginId =
            _platform.Username
            ?? _session.CurrentAccount?.Username
            ?? string.Empty;
        CurrentPassword = NewPassword = ConfirmPassword = string.Empty;
        LoginMessage = string.Empty;
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        CompanyError = OwnerError = PhoneError = EmailError = string.Empty;
        var valid = true;

        if (!ValidationHelper.IsRequired(CompanyName) || CompanyName.Trim().Length < 2)
        {
            CompanyError = "Company / shop name is required.";
            valid = false;
        }

        if (!ValidationHelper.IsValidName(OwnerName))
        {
            OwnerError = "Enter owner full name.";
            valid = false;
        }

        if (!ValidationHelper.IsValidPhone(Phone))
        {
            PhoneError = "Enter a valid business phone.";
            valid = false;
        }

        if (!ValidationHelper.IsValidOptionalEmail(Email))
        {
            EmailError = "Enter a valid email or leave blank.";
            valid = false;
        }

        if (!valid)
        {
            FormMessage = "Please complete required settings.";
            FormMessageBrush = new SolidColorBrush(Color.FromRgb(220, 38, 38));
            return;
        }

        await _settings.SaveAsync(BuildSettings());
        FormMessage = "Settings saved. Report header will use these details.";
        FormMessageBrush = new SolidColorBrush(Color.FromRgb(5, 150, 105));
    }

    [RelayCommand]
    private async Task ChangeLoginAsync()
    {
        LoginMessage = string.Empty;

        if (string.IsNullOrWhiteSpace(CurrentLoginId))
        {
            SetLoginError("Login ID is missing. Sign out and sign in again.");
            return;
        }

        if (string.IsNullOrWhiteSpace(_platform.Token))
        {
            SetLoginError("Session expired. Sign out and sign in again, then change password.");
            return;
        }

        if (string.IsNullOrWhiteSpace(CurrentPassword))
        {
            SetLoginError("Enter your current password.");
            return;
        }

        if (string.IsNullOrWhiteSpace(NewPassword) || NewPassword.Length < 6)
        {
            SetLoginError("New password must be at least 6 characters.");
            return;
        }

        if (NewPassword != ConfirmPassword)
        {
            SetLoginError("New password and confirm password do not match.");
            return;
        }

        IsChangingPassword = true;
        try
        {
            var (message, error) = await _api.ChangeClientPasswordAsync(
                new ChangeClientPasswordRequest
                {
                    CurrentPassword = CurrentPassword,
                    NewPassword = NewPassword
                },
                _platform.Token);

            if (!string.IsNullOrWhiteSpace(error))
            {
                SetLoginError(error);
                return;
            }

            CurrentPassword = NewPassword = ConfirmPassword = string.Empty;
            LoginMessage = message ?? "Password updated. Your Login ID stays the same.";
            LoginMessageBrush = new SolidColorBrush(Color.FromRgb(5, 150, 105));
            MessageBox.Show(
                $"Password updated successfully.\n\nLogin ID (unchanged): {CurrentLoginId}\nUse this Login ID with your new password next time.",
                "Password Updated",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            SetLoginError(ex.Message);
        }
        finally
        {
            IsChangingPassword = false;
        }
    }

    private void SetLoginError(string message)
    {
        LoginMessage = message;
        LoginMessageBrush = new SolidColorBrush(Color.FromRgb(220, 38, 38));
    }

    private CompanySettings BuildSettings() => new()
    {
        Id = 1,
        BusinessType = string.IsNullOrWhiteSpace(BusinessType) ? "General rental" : BusinessType.Trim(),
        CompanyName = CompanyName.Trim(),
        OwnerName = OwnerName.Trim(),
        Address = Address.Trim(),
        City = City.Trim(),
        Phone = Phone.Trim(),
        Email = Email.Trim(),
        GstNumber = GstNumber.Trim(),
        ShowGstOnReports = ShowGstOnReports,
        Currency = string.IsNullOrWhiteSpace(Currency) ? "INR (₹)" : Currency.Trim(),
        ReportFooter = string.IsNullOrWhiteSpace(ReportFooter) ? "Thank you for your business" : ReportFooter.Trim(),
        EnableNotifications = EnableNotifications,
        AutoBackup = AutoBackup
    };
}
