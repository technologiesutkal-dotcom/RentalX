using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RentalX.Data.Repositories;
using RentalX.Helpers;
using RentalX.Models;
using RentalX.Services;
using RentalX.ViewModels.Base;

namespace RentalX.ViewModels;

public partial class ReturnsViewModel : PageViewModelBase
{
    private readonly IBookingRepository _bookings;
    private readonly IRentalOperationsService _operations;
    private List<Booking> _all = [];
    private bool _suppressSettlementRefresh;

    public ReturnsViewModel(IBookingRepository bookings, IRentalOperationsService operations)
    {
        _bookings = bookings;
        _operations = operations;
        CollectPayment = true;
        GenerateInvoice = true;
        ApplyEarlyCredit = true;
        ActualReturnDate = DateTime.Today;
        ManualAdjustmentText = "0";
    }

    public ObservableCollection<Booking> PendingReturns { get; } = new();

    public string WorkflowHint { get; } =
        "Receive & settle: Late return → extra days charged automatically. Early return → credit (optional). " +
        "You can also type a Manual adjust (+/-). Then settle money (collect balance or refund excess advance).";

    [ObservableProperty] private Booking? _selectedBooking;
    [ObservableProperty] private bool _collectPayment;
    [ObservableProperty] private bool _generateInvoice;
    [ObservableProperty] private bool _applyEarlyCredit;
    [ObservableProperty] private DateTime? _actualReturnDate;
    [ObservableProperty] private string _manualAdjustmentText = "0";
    [ObservableProperty] private string _settlementNote = string.Empty;
    [ObservableProperty] private ReturnSettlement? _settlement;
    [ObservableProperty] private string _searchText = string.Empty;
    [ObservableProperty] private string _resultCountText = "0 pending";
    [ObservableProperty] private string _settlementHint = string.Empty;
    [ObservableProperty] private string _collectRefundText = "-";
    [ObservableProperty] private string _formMessage = string.Empty;
    [ObservableProperty] private Brush _formMessageBrush = Brushes.Transparent;

    partial void OnSearchTextChanged(string value) => ApplyFilter();
    partial void OnSelectedBookingChanged(Booking? value) => _ = RefreshSettlementAsync();
    partial void OnActualReturnDateChanged(DateTime? value) => _ = RefreshSettlementAsync();
    partial void OnManualAdjustmentTextChanged(string value) => _ = RefreshSettlementAsync();
    partial void OnApplyEarlyCreditChanged(bool value) => _ = RefreshSettlementAsync();

    public override async Task LoadAsync()
    {
        IsBusy = true;
        await _operations.MarkOverdueBookingsAsync();
        var dispatched = await _bookings.GetByStatusAsync("Dispatched");
        var overdue = await _bookings.GetByStatusAsync("Overdue");
        _all = dispatched.Concat(overdue)
            .OrderBy(x => x.ExpectedReturnDate ?? x.ReturnDate ?? DateTime.MaxValue)
            .ToList();
        ApplyFilter();
        await RefreshSettlementAsync();
        IsBusy = false;
    }

    private void ApplyFilter()
    {
        var filtered = _all.Where(b =>
            ListFilterHelper.Matches(
                SearchText,
                b.BookingNumber,
                b.CustomerName,
                b.ItemsSummary,
                b.AmountText,
                b.BalanceText,
                b.StatusText,
                b.ExpectedReturnText));
        ListFilterHelper.Replace(PendingReturns, filtered);
        ResultCountText = $"{PendingReturns.Count} pending";
    }

    [RelayCommand]
    private void ClearSearch() => SearchText = string.Empty;

    [RelayCommand]
    private void ResetManual()
    {
        ManualAdjustmentText = "0";
        SettlementNote = string.Empty;
        ActualReturnDate = DateTime.Today;
        ApplyEarlyCredit = true;
    }

    [RelayCommand]
    private async Task RefreshSettlementCommandAsync() => await RefreshSettlementAsync();

    private async Task RefreshSettlementAsync()
    {
        if (_suppressSettlementRefresh)
        {
            return;
        }

        if (SelectedBooking is null)
        {
            Settlement = null;
            CollectRefundText = "-";
            SettlementHint = "Select a booking to see late / early settlement.";
            return;
        }

        if (!ActualReturnDate.HasValue)
        {
            Settlement = null;
            CollectRefundText = "-";
            SettlementHint = "Select actual return date.";
            return;
        }

        if (!decimal.TryParse(string.IsNullOrWhiteSpace(ManualAdjustmentText) ? "0" : ManualAdjustmentText, out var manual))
        {
            SettlementHint = "Manual adjust must be a number (use minus for discount).";
            return;
        }

        try
        {
            Settlement = await _operations.PreviewReturnSettlementAsync(
                SelectedBooking.Id,
                ActualReturnDate.Value,
                manual,
                ApplyEarlyCredit);

            CollectRefundText = Settlement.RefundDue > 0
                ? $"Refund {Settlement.RefundDueText}"
                : $"Collect {Settlement.AmountToCollectText}";

            SettlementHint = Settlement.SettlementType switch
            {
                "Late" => $"Auto late charge {Settlement.AutoAdjustmentText}. Collect {Settlement.AmountToCollectText} to close.",
                "Early" when ApplyEarlyCredit && Settlement.RefundDue > 0 =>
                    $"Early return credit applied. Refund due {Settlement.RefundDueText}.",
                "Early" when ApplyEarlyCredit =>
                    $"Early return credit applied. Collect remaining {Settlement.AmountToCollectText}.",
                "Early" => "Early return — original amount kept (no credit).",
                _ => Settlement.AmountToCollect > 0
                    ? $"On time. Collect remaining {Settlement.AmountToCollectText}."
                    : "On time. Nothing more to collect."
            };
        }
        catch (Exception ex)
        {
            Settlement = null;
            CollectRefundText = "-";
            SettlementHint = ex.Message;
        }
    }

    [RelayCommand]
    private async Task ReceiveSelectedAsync()
    {
        if (SelectedBooking is null)
        {
            FormMessage = "Select a booking where items are out with the customer.";
            FormMessageBrush = new SolidColorBrush(Color.FromRgb(220, 38, 38));
            return;
        }

        if (!ActualReturnDate.HasValue)
        {
            FormMessage = "Select actual return date.";
            FormMessageBrush = new SolidColorBrush(Color.FromRgb(220, 38, 38));
            return;
        }

        if (!decimal.TryParse(string.IsNullOrWhiteSpace(ManualAdjustmentText) ? "0" : ManualAdjustmentText, out var manual))
        {
            FormMessage = "Manual adjust must be a valid number.";
            FormMessageBrush = new SolidColorBrush(Color.FromRgb(220, 38, 38));
            return;
        }

        try
        {
            var preview = await _operations.PreviewReturnSettlementAsync(
                SelectedBooking.Id,
                ActualReturnDate.Value,
                manual,
                ApplyEarlyCredit);

            var confirm = MessageBox.Show(
                $"{preview.BookingNumber} — {preview.TypeLabel}\n\n" +
                $"Original: {preview.OriginalAmountText}\n" +
                $"Auto adjust: {preview.AutoAdjustmentText}\n" +
                $"Manual: ₹{manual:N0}\n" +
                $"Final: {preview.FinalAmountText}\n" +
                $"Already paid: ₹{preview.PaidAmount:N0}\n" +
                (preview.AmountToCollect > 0
                    ? $"To collect now: {preview.AmountToCollectText}\n"
                    : preview.RefundDue > 0
                        ? $"Refund due: {preview.RefundDueText}\n"
                        : "No money movement needed.\n") +
                "\nReceive items and apply this settlement?",
                "Confirm Receive & Settlement",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (confirm != MessageBoxResult.Yes)
            {
                return;
            }

            var updated = await _operations.ReceiveBookingAsync(
                SelectedBooking.Id,
                ActualReturnDate.Value,
                manual,
                ApplyEarlyCredit,
                CollectPayment,
                GenerateInvoice,
                SettlementNote);

            FormMessage = $"{updated.BookingNumber} received. {updated.SettlementNote}";
            if (CollectPayment)
            {
                if (preview.AmountToCollect > 0)
                {
                    FormMessage += $" Collected {preview.AmountToCollectText}.";
                }
                else if (preview.RefundDue > 0)
                {
                    FormMessage += $" Refund recorded {preview.RefundDueText}.";
                }
            }

            if (GenerateInvoice)
            {
                var path = await _operations.GenerateInvoicePdfAsync(updated.Id);
                FormMessage += $" Invoice {updated.InvoiceNumber} saved.";
                MessageBox.Show($"{FormMessage}\n\nInvoice:\n{path}", "Receive & Settlement", MessageBoxButton.OK, MessageBoxImage.Information);
            }

            FormMessageBrush = new SolidColorBrush(Color.FromRgb(5, 150, 105));
            _suppressSettlementRefresh = true;
            SelectedBooking = null;
            Settlement = null;
            ManualAdjustmentText = "0";
            SettlementNote = string.Empty;
            ActualReturnDate = DateTime.Today;
            _suppressSettlementRefresh = false;
            await LoadAsync();
        }
        catch (Exception ex)
        {
            FormMessage = ex.Message;
            FormMessageBrush = new SolidColorBrush(Color.FromRgb(220, 38, 38));
        }
    }
}
