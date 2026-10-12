using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RentalX.Services.Licensing;

namespace RentalX.ViewModels;

public partial class ActivationViewModel : ObservableObject
{
    private readonly ILicenseClientService _license;
    private readonly IMachineIdService _machine;
    private readonly Window _window;

    [ObservableProperty] private string _apiBaseUrl = "http://localhost:5159";
    [ObservableProperty] private string _licenseKey = string.Empty;
    [ObservableProperty] private string _machineId = string.Empty;
    [ObservableProperty] private string _machineName = string.Empty;
    [ObservableProperty] private string? _errorMessage;
    [ObservableProperty] private string? _infoMessage;
    [ObservableProperty] private bool _isBusy;

    public ActivationViewModel(ILicenseClientService license, IMachineIdService machine, Window window)
    {
        _license = license;
        _machine = machine;
        _window = window;
        MachineId = machine.GetMachineId();
        MachineName = machine.GetMachineName();
        ApiBaseUrl = license.ApiBaseUrl;
        if (license.Current is not null)
        {
            LicenseKey = license.Current.LicenseKey;
            InfoMessage = $"Previously activated for {license.Current.CompanyName ?? "client"}.";
        }
    }

    [RelayCommand]
    private async Task ActivateAsync()
    {
        ErrorMessage = null;
        InfoMessage = null;

        if (string.IsNullOrWhiteSpace(ApiBaseUrl))
        {
            ErrorMessage = "API URL is required.";
            return;
        }

        if (string.IsNullOrWhiteSpace(LicenseKey) || LicenseKey.Trim().Length < 8)
        {
            ErrorMessage = "Enter the license key provided by your vendor.";
            return;
        }

        IsBusy = true;
        try
        {
            _license.ApiBaseUrl = ApiBaseUrl.Trim();
            var (ok, message) = await _license.ActivateAsync(LicenseKey);
            if (!ok)
            {
                ErrorMessage = message;
                return;
            }

            _window.DialogResult = true;
            _window.Close();
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void Exit()
    {
        _window.DialogResult = false;
        _window.Close();
    }

    [RelayCommand]
    private void CopyMachineId()
    {
        Clipboard.SetText(MachineId);
        InfoMessage = "Machine ID copied.";
    }
}
