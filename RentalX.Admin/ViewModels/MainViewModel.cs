using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RentalX.Admin.Helpers;
using RentalX.Admin.Models;
using RentalX.Admin.Services;

namespace RentalX.Admin.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly IAdminSession _session;
    private readonly ILicenseApiClient _api;

    [ObservableProperty] private object? _currentPage;
    [ObservableProperty] private string _pageTitle = "Dashboard";
    [ObservableProperty] private string _currentUserName = "Admin";
    [ObservableProperty] private string _currentUserInitial = "A";
    [ObservableProperty] private string _apiStatus = "Neon PostgreSQL";

    public ObservableCollection<NavItem> NavItems { get; } =
    [
        new() { Key = "serials", Title = "Requests", Icon = "#" },
        new() { Key = "dashboard", Title = "Dashboard", Icon = "◫" },
        new() { Key = "clients", Title = "Clients", Icon = "◎" },
        new() { Key = "licenses", Title = "Licenses", Icon = "🔑" },
        new() { Key = "payments", Title = "Payments", Icon = "₹" },
        new() { Key = "account", Title = "Account", Icon = "⚙" }
    ];

    public MainViewModel(IAdminSession session, ILicenseApiClient api)
    {
        _session = session;
        _api = api;
        CurrentUserName = session.DisplayName ?? "Super Admin";
        CurrentUserInitial = string.IsNullOrWhiteSpace(CurrentUserName)
            ? "A"
            : CurrentUserName.Trim()[0].ToString().ToUpperInvariant();
        Navigate("serials");
        _ = RefreshDbStatusAsync();
    }

    [RelayCommand]
    private async Task RefreshDbStatusAsync()
    {
        try
        {
            var (data, error) = await _api.GetDbHealthAsync();
            if (data is { Ok: true })
            {
                var label = string.IsNullOrWhiteSpace(data.DatabaseLabel)
                    ? "Neon PostgreSQL"
                    : data.DatabaseLabel;
                ApiStatus = $"{label} · Connected";
                return;
            }

            ApiStatus = "Neon PostgreSQL · Unreachable";
        }
        catch
        {
            ApiStatus = "Neon PostgreSQL · Unreachable";
        }
    }

    [RelayCommand]
    private void Navigate(string? key)
    {
        key = (key ?? "serials").ToLowerInvariant();
        switch (key)
        {
            case "serials":
                PageTitle = "Access requests";
                CurrentPage = ServiceHelper.GetRequiredService<SerialsViewModel>();
                break;
            case "clients":
                PageTitle = "Clients";
                CurrentPage = ServiceHelper.GetRequiredService<ClientsViewModel>();
                break;
            case "licenses":
                PageTitle = "Licenses";
                CurrentPage = ServiceHelper.GetRequiredService<LicensesViewModel>();
                break;
            case "payments":
                PageTitle = "Payments";
                CurrentPage = ServiceHelper.GetRequiredService<PaymentsViewModel>();
                break;
            case "account":
                PageTitle = "Account";
                CurrentPage = ServiceHelper.GetRequiredService<AccountViewModel>();
                break;
            default:
                PageTitle = "Dashboard";
                CurrentPage = ServiceHelper.GetRequiredService<DashboardViewModel>();
                break;
        }

        _ = RefreshDbStatusAsync();
    }

    [RelayCommand]
    private async Task SignOutAsync()
    {
        var confirm = MessageBox.Show(
            "Sign out of Super Admin?",
            "RentalX Admin",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);
        if (confirm != MessageBoxResult.Yes)
        {
            return;
        }

        await _api.LogoutAsync();
        _session.Clear();
    }
}
