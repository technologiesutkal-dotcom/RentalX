using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RentalX.Data.Repositories;
using RentalX.Models;
using RentalX.Services;
using RentalX.ViewModels.Base;

namespace RentalX.ViewModels;

public partial class DashboardViewModel : PageViewModelBase
{
    private readonly IDashboardRepository _dashboard;
    private readonly IBookingRepository _bookings;
    private readonly IInventoryRepository _inventory;
    private readonly INavigationService _navigation;

    public DashboardViewModel(
        IDashboardRepository dashboard,
        IBookingRepository bookings,
        IInventoryRepository inventory,
        INavigationService navigation)
    {
        _dashboard = dashboard;
        _bookings = bookings;
        _inventory = inventory;
        _navigation = navigation;
    }

    [ObservableProperty]
    private DashboardSummary _summary = new();

    public ObservableCollection<Booking> RecentBookings { get; } = new();
    public ObservableCollection<InventoryItem> StockItems { get; } = new();
    public ObservableCollection<UpcomingReturnItem> UpcomingReturns { get; } = new();
    public ObservableCollection<LowStockItem> LowStockItems { get; } = new();

    public string TotalItemsText => Summary.TotalItems.ToString("N0");
    public string AvailableItemsText => Summary.AvailableItems.ToString("N0");
    public string ActiveRentalsText => Summary.ActiveRentals.ToString("N0");
    public string OverdueCountText => Summary.OverdueCount.ToString("N0");
    public string TodaysRevenueText => $"₹{Summary.TodaysRevenue:N0}";
    public string RevenueGrowthText => $"+{Summary.RevenueGrowthPercent:0.#}% today";
    public string CollectedText => $"₹{Summary.CollectedToday:N0}";
    public string PendingText => $"₹{Summary.PendingAmount:N0}";
    public string DepositsText => $"₹{Summary.DepositsHeld:N0}";

    public override async Task LoadAsync()
    {
        try
        {
            IsBusy = true;
            Summary = await _dashboard.GetSummaryAsync();

            RecentBookings.Clear();
            foreach (var booking in await _bookings.GetRecentAsync(4))
            {
                RecentBookings.Add(booking);
            }

            StockItems.Clear();
            foreach (var item in await _inventory.GetAllAsync())
            {
                StockItems.Add(item);
            }

            UpcomingReturns.Clear();
            foreach (var item in await _dashboard.GetUpcomingReturnsAsync())
            {
                UpcomingReturns.Add(item);
            }

            LowStockItems.Clear();
            foreach (var item in await _dashboard.GetLowStockAsync())
            {
                LowStockItems.Add(item);
            }

            OnPropertyChanged(nameof(TotalItemsText));
            OnPropertyChanged(nameof(AvailableItemsText));
            OnPropertyChanged(nameof(ActiveRentalsText));
            OnPropertyChanged(nameof(OverdueCountText));
            OnPropertyChanged(nameof(TodaysRevenueText));
            OnPropertyChanged(nameof(RevenueGrowthText));
            OnPropertyChanged(nameof(CollectedText));
            OnPropertyChanged(nameof(PendingText));
            OnPropertyChanged(nameof(DepositsText));
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private Task GoToBookingsAsync() => _navigation.NavigateToAsync("Bookings");

    [RelayCommand]
    private Task GoToInventoryAsync() => _navigation.NavigateToAsync("Inventory");

    [RelayCommand]
    private Task GoToDispatchAsync() => _navigation.NavigateToAsync("Dispatch");

    [RelayCommand]
    private Task GoToReturnsAsync() => _navigation.NavigateToAsync("Returns");

    [RelayCommand]
    private Task GoToBillingAsync() => _navigation.NavigateToAsync("Billing");
}
