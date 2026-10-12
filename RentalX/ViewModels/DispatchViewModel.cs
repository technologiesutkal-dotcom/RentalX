using System.Collections.ObjectModel;
using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RentalX.Data.Repositories;
using RentalX.Helpers;
using RentalX.Models;
using RentalX.Services;
using RentalX.ViewModels.Base;

namespace RentalX.ViewModels;

public partial class DispatchViewModel : PageViewModelBase
{
    private readonly IBookingRepository _bookings;
    private readonly IRentalOperationsService _operations;
    private List<Booking> _all = [];

    public DispatchViewModel(IBookingRepository bookings, IRentalOperationsService operations)
    {
        _bookings = bookings;
        _operations = operations;
    }

    public ObservableCollection<Booking> ReadyToDispatch { get; } = new();

    public string WorkflowHint { get; } =
        "Step 2 of 3 — DISPATCH (Give items): When customer takes the items, click Dispatch. " +
        "Stock leaves your warehouse (Available ↓). Money is NOT collected here — use Booking advance or Receive/Payments.";

    [ObservableProperty] private Booking? _selectedBooking;
    [ObservableProperty] private string _searchText = string.Empty;
    [ObservableProperty] private string _resultCountText = "0 ready";
    [ObservableProperty] private string _formMessage = string.Empty;
    [ObservableProperty] private Brush _formMessageBrush = Brushes.Transparent;

    partial void OnSearchTextChanged(string value) => ApplyFilter();

    public override async Task LoadAsync()
    {
        IsBusy = true;
        _all = await _bookings.GetByStatusAsync("Confirmed");
        ApplyFilter();
        IsBusy = false;
    }

    private void ApplyFilter()
    {
        var filtered = _all.Where(b =>
            ListFilterHelper.Matches(SearchText, b.BookingNumber, b.CustomerName, b.ItemsSummary, b.AmountText, b.BalanceText));
        ListFilterHelper.Replace(ReadyToDispatch, filtered);
        ResultCountText = $"{ReadyToDispatch.Count} ready";
    }

    [RelayCommand]
    private void ClearSearch() => SearchText = string.Empty;

    [RelayCommand]
    private async Task DispatchSelectedAsync()
    {
        if (SelectedBooking is null)
        {
            FormMessage = "Select a booked order to give items to the customer.";
            FormMessageBrush = new SolidColorBrush(Color.FromRgb(220, 38, 38));
            return;
        }

        try
        {
            await _operations.DispatchBookingAsync(SelectedBooking.Id);
            FormMessage =
                $"{SelectedBooking.BookingNumber} dispatched — items given to customer. " +
                "Stock reduced. When they return items, open Receive.";
            FormMessageBrush = new SolidColorBrush(Color.FromRgb(5, 150, 105));
            await LoadAsync();
        }
        catch (Exception ex)
        {
            FormMessage = ex.Message;
            FormMessageBrush = new SolidColorBrush(Color.FromRgb(220, 38, 38));
        }
    }
}
