using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RentalX.Admin.Services;

namespace RentalX.Admin.ViewModels;

public partial class DashboardViewModel : ObservableObject
{
    private readonly ILicenseApiClient _api;

    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private string? _errorMessage;
    [ObservableProperty] private int _totalClients;
    [ObservableProperty] private int _activeClients;
    [ObservableProperty] private int _activeLicenses;
    [ObservableProperty] private int _expiringSoon;
    [ObservableProperty] private int _activeActivations;
    [ObservableProperty] private decimal _revenueThisMonth;
    [ObservableProperty] private decimal _revenueTotal;
    [ObservableProperty] private int _pendingPayments;
    [ObservableProperty] private int _pendingSerials;

    public DashboardViewModel(ILicenseApiClient api)
    {
        _api = api;
        _ = LoadAsync();
    }

    [RelayCommand]
    private async Task LoadAsync()
    {
        IsBusy = true;
        ErrorMessage = null;
        try
        {
            var (data, error) = await _api.GetDashboardAsync();
            if (data is null)
            {
                ErrorMessage = error ?? "Failed to load dashboard.";
                return;
            }

            TotalClients = data.TotalClients;
            ActiveClients = data.ActiveClients;
            ActiveLicenses = data.ActiveLicenses;
            ExpiringSoon = data.ExpiringSoon;
            ActiveActivations = data.ActiveActivations;
            RevenueThisMonth = data.RevenueThisMonth;
            RevenueTotal = data.RevenueTotal;
            PendingPayments = data.PendingPayments;
            PendingSerials = data.PendingSerials;
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task FactoryResetAsync()
    {
        var confirm = MessageBox.Show(
            "FULL CLEANUP?\n\n" +
            "This deletes ALL clients, licenses, requests, payments, activations.\n" +
            "Only your Super Admin login will remain (username/password unchanged).\n\nContinue?",
            "Full cleanup",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (confirm != MessageBoxResult.Yes)
        {
            return;
        }

        IsBusy = true;
        ErrorMessage = null;
        try
        {
            var (message, error) = await _api.FactoryResetAsync();
            if (error is not null)
            {
                ErrorMessage = error;
                return;
            }

            await LoadAsync();
            MessageBox.Show(
                message ?? "Cleanup done. Super Admin login unchanged.",
                "Clean — Super Admin only",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
        finally
        {
            IsBusy = false;
        }
    }
}
