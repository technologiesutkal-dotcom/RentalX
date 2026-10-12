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

public partial class PaymentsViewModel : PageViewModelBase
{
    private readonly IPaymentRepository _payments;
    private readonly IBookingRepository _bookings;
    private readonly IRentalOperationsService _operations;
    private List<Booking> _allUnpaid = [];
    private List<Payment> _allPayments = [];

    public PaymentsViewModel(
        IPaymentRepository payments,
        IBookingRepository bookings,
        IRentalOperationsService operations)
    {
        _payments = payments;
        _bookings = bookings;
        _operations = operations;
    }

    public ObservableCollection<Payment> Payments { get; } = new();
    public ObservableCollection<Booking> UnpaidBookings { get; } = new();

    public string WorkflowHint { get; } =
        "Collected Today = Advance + Balance + Late charges received today. " +
        "Refunded Today = Early-return refunds paid out today. " +
        "Pending = booking balances still due. Net cash today = Collected − Refunded.";

    [ObservableProperty] private string _collectedText = "₹0";
    [ObservableProperty] private string _pendingText = "₹0";
    [ObservableProperty] private string _refundedText = "₹0";
    [ObservableProperty] private string _netTodayText = "₹0";
    [ObservableProperty] private Booking? _selectedUnpaidBooking;
    [ObservableProperty] private string _collectAmountText = string.Empty;
    [ObservableProperty] private string _searchText = string.Empty;
    [ObservableProperty] private string _resultCountText = "0";
    [ObservableProperty] private string _formMessage = string.Empty;
    [ObservableProperty] private Brush _formMessageBrush = Brushes.Transparent;

    partial void OnSearchTextChanged(string value) => ApplyFilter();

    partial void OnSelectedUnpaidBookingChanged(Booking? value)
    {
        CollectAmountText = value is null ? string.Empty : value.BalanceDue.ToString("0");
    }

    public override async Task LoadAsync()
    {
        IsBusy = true;
        _allPayments = await _payments.GetAllAsync();
        var bookings = await _bookings.GetAllAsync();
        _allUnpaid = bookings
            .Where(x => x.BalanceDue > 0 && x.Status != Models.Enums.BookingStatus.Cancelled)
            .ToList();

        ApplyFilter();
        SelectedUnpaidBooking ??= UnpaidBookings.FirstOrDefault();
        var collected = await _payments.GetCollectedTodayAsync();
        var refunded = await _payments.GetRefundedTodayAsync();
        CollectedText = $"₹{collected:N0}";
        PendingText = $"₹{_allUnpaid.Sum(x => x.BalanceDue):N0}";
        RefundedText = $"₹{refunded:N0}";
        NetTodayText = $"₹{(collected - refunded):N0}";
        IsBusy = false;
    }

    private void ApplyFilter()
    {
        var unpaid = _allUnpaid.Where(b =>
            ListFilterHelper.Matches(SearchText, b.BookingNumber, b.CustomerName, b.ItemsSummary, b.AmountText, b.BalanceText, b.StatusText));
        ListFilterHelper.Replace(UnpaidBookings, unpaid);

        var history = _allPayments.Where(p =>
            ListFilterHelper.Matches(SearchText, p.Description, p.AmountText, p.StatusText, p.Booking?.BookingNumber, p.Booking?.CustomerName));
        ListFilterHelper.Replace(Payments, history);
        ResultCountText = $"{UnpaidBookings.Count} unpaid · {Payments.Count} history";
    }

    [RelayCommand]
    private void ClearSearch() => SearchText = string.Empty;

    [RelayCommand]
    private async Task CollectSelectedAsync()
    {
        if (SelectedUnpaidBooking is null)
        {
            FormMessage = "Select a booking with remaining balance.";
            FormMessageBrush = new SolidColorBrush(Color.FromRgb(220, 38, 38));
            return;
        }

        if (!ValidationHelper.IsValidPositiveDecimal(CollectAmountText, out var amount))
        {
            FormMessage = "Enter a valid amount to collect.";
            FormMessageBrush = new SolidColorBrush(Color.FromRgb(220, 38, 38));
            return;
        }

        try
        {
            await _operations.CollectBalanceAsync(SelectedUnpaidBooking.Id, amount);
            FormMessage = $"Collected ₹{amount:N0} for {SelectedUnpaidBooking.BookingNumber}.";
            FormMessageBrush = new SolidColorBrush(Color.FromRgb(5, 150, 105));
            await LoadAsync();
        }
        catch (Exception ex)
        {
            FormMessage = ex.Message;
            FormMessageBrush = new SolidColorBrush(Color.FromRgb(220, 38, 38));
        }
    }

    [RelayCommand]
    private async Task CollectFullBalanceAsync()
    {
        if (SelectedUnpaidBooking is null)
        {
            FormMessage = "Select a booking with remaining balance.";
            FormMessageBrush = new SolidColorBrush(Color.FromRgb(220, 38, 38));
            return;
        }

        try
        {
            var balance = SelectedUnpaidBooking.BalanceDue;
            await _operations.CollectBalanceAsync(SelectedUnpaidBooking.Id);
            FormMessage = $"Collected full remaining ₹{balance:N0} for {SelectedUnpaidBooking.BookingNumber}.";
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
