using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.DependencyInjection;
using RentalX.ViewModels;
using RentalX.ViewModels.Base;

namespace RentalX.Services;

public class NavigationService : INavigationService
{
    private readonly IServiceProvider _services;

    private static readonly Dictionary<string, (Type ViewModelType, string Title, string Subtitle)> Pages = new()
    {
        ["Dashboard"] = (typeof(DashboardViewModel), "Dashboard", "Overview of your rental business"),
        ["Bookings"] = (typeof(BookingsViewModel), "Bookings", "Manage rental bookings and schedules"),
        ["Inventory"] = (typeof(InventoryViewModel), "Inventory", "Track stock levels and item availability"),
        ["Dispatch"] = (typeof(DispatchViewModel), "Dispatch", "Send rented items to customers"),
        ["Returns"] = (typeof(ReturnsViewModel), "Returns", "Receive and inspect returned items"),
        ["Billing"] = (typeof(BillingViewModel), "Billing", "Create and manage invoices"),
        ["Payments"] = (typeof(PaymentsViewModel), "Payments", "Track collections and pending dues"),
        ["Expenses"] = (typeof(ExpensesViewModel), "Expenses", "Track business expenses and costs"),
        ["Maintenance"] = (typeof(MaintenanceViewModel), "Maintenance", "Repair and service inventory"),
        ["Customers"] = (typeof(CustomersViewModel), "Customers", "Customer directory and contacts"),
        ["Reports"] = (typeof(ReportsViewModel), "Reports", "Profit, balance and PDF reports"),
        ["Settings"] = (typeof(SettingsViewModel), "Settings", "Company / tent details for reports"),
        ["Users"] = (typeof(UsersViewModel), "Staff", "Manage rental organization staff")
    };

    public NavigationService(IServiceProvider services)
    {
        _services = services;
    }

    public ObservableObject? CurrentViewModel { get; private set; }
    public string CurrentPageKey { get; private set; } = "Dashboard";
    public string CurrentPageTitle { get; private set; } = "Dashboard";
    public string CurrentPageSubtitle { get; private set; } = "Overview of your rental business";

    public event Action? Navigated;
    public event Action<bool, string>? LoadingChanged;

    public async Task NavigateToAsync(string pageKey)
    {
        if (!Pages.TryGetValue(pageKey, out var page))
        {
            page = Pages["Dashboard"];
            pageKey = "Dashboard";
        }

        LoadingChanged?.Invoke(true, $"Loading {page.Title}...");

        try
        {
            await Task.Delay(300);

            CurrentPageKey = pageKey;
            CurrentPageTitle = page.Title;
            CurrentPageSubtitle = page.Subtitle;
            CurrentViewModel = (ObservableObject)_services.GetRequiredService(page.ViewModelType);
            Navigated?.Invoke();

            if (CurrentViewModel is PageViewModelBase pageVm)
            {
                await pageVm.LoadAsync();
            }

            await Task.Delay(120);
        }
        finally
        {
            LoadingChanged?.Invoke(false, string.Empty);
        }
    }
}
