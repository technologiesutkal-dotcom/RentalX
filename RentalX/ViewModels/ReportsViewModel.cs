using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RentalX.Data.Repositories;
using RentalX.Helpers;
using RentalX.Models;
using RentalX.Models.Enums;
using RentalX.Services;
using RentalX.ViewModels.Base;

namespace RentalX.ViewModels;

public partial class ReportsViewModel : PageViewModelBase
{
    private readonly IBookingRepository _bookings;
    private readonly IPaymentRepository _payments;
    private readonly IExpenseRepository _expenses;
    private readonly ISettingsRepository _settings;
    private readonly IPdfReportService _pdf;

    public ReportsViewModel(
        IBookingRepository bookings,
        IPaymentRepository payments,
        IExpenseRepository expenses,
        ISettingsRepository settings,
        IPdfReportService pdf)
    {
        _bookings = bookings;
        _payments = payments;
        _expenses = expenses;
        _settings = settings;
        _pdf = pdf;
        FromDate = DateTime.Today.AddDays(-30);
        ToDate = DateTime.Today;
    }

    public ObservableCollection<ReportBookingRow> ReportRows { get; } = new();
    public ObservableCollection<Expense> ExpenseRows { get; } = new();

    [ObservableProperty] private DateTime? _fromDate;
    [ObservableProperty] private DateTime? _toDate;
    [ObservableProperty] private string _totalBookingsText = "0";
    [ObservableProperty] private string _totalRevenueText = "₹0";
    [ObservableProperty] private string _collectedText = "₹0";
    [ObservableProperty] private string _pendingText = "₹0";
    [ObservableProperty] private string _expensesText = "₹0";
    [ObservableProperty] private string _profitText = "₹0";
    [ObservableProperty] private string _balanceText = "₹0";
    [ObservableProperty] private string _overdueText = "0";
    [ObservableProperty] private string _dispatchedText = "0";
    [ObservableProperty] private string _reportPeriodText = "Select dates and generate report";
    [ObservableProperty] private string _companyHeaderText = "RentalX";
    [ObservableProperty] private ReportBookingRow? _selectedRow;
    [ObservableProperty] private ReportSummary _currentSummary = new();

    public override async Task LoadAsync()
    {
        var company = await _settings.GetAsync();
        CompanyHeaderText = $"{company.CompanyName} · {company.OwnerName}";
        await GenerateReportAsync();
    }

    [RelayCommand]
    private async Task GenerateReportAsync()
    {
        if (!ValidationHelper.IsValidDateRange(FromDate, ToDate))
        {
            StatusMessage = "Select a valid From and To date (From cannot be after To).";
            return;
        }

        var from = FromDate!.Value.Date;
        var to = ToDate!.Value.Date;
        if (to > DateTime.Today)
        {
            StatusMessage = "To date cannot be in the future.";
            return;
        }

        IsBusy = true;
        StatusMessage = "Generating report...";

        try
        {
            var bookings = await _bookings.GetByDateRangeAsync(from, to);
            var payments = await _payments.GetByDateRangeAsync(from, to);
            var expenses = await _expenses.GetByDateRangeAsync(from, to);
            var company = await _settings.GetAsync();
            CompanyHeaderText = $"{company.CompanyName} · {company.OwnerName} · {company.City}";

            ReportRows.Clear();
            foreach (var booking in bookings)
            {
                ReportRows.Add(new ReportBookingRow
                {
                    BookingNumber = booking.BookingNumber,
                    CustomerName = booking.CustomerName,
                    ItemsSummary = booking.ItemsSummary,
                    BookingDate = booking.BookingDate,
                    Amount = booking.Amount,
                    StatusText = booking.StatusText
                });
            }

            ExpenseRows.Clear();
            foreach (var expense in expenses)
            {
                ExpenseRows.Add(expense);
            }

            // Collected includes advances + balance + late; refunds reduce cash.
            var collected = payments.Where(x => x.Status == PaymentStatus.Collected).Sum(x => x.Amount);
            var refunded = payments.Where(x => x.Status == PaymentStatus.Refunded).Sum(x => x.Amount);
            var netCollected = collected - refunded;
            var pending = payments.Where(x => x.Status == PaymentStatus.Pending && !x.IsDeposit).Sum(x => x.Amount);
            var expenseTotal = expenses.Sum(x => x.Amount);
            var bookingValue = bookings.Sum(x => x.Amount);
            var profit = netCollected - expenseTotal;
            var balance = netCollected - pending - expenseTotal;

            CurrentSummary = new ReportSummary
            {
                TotalBookings = bookings.Count,
                TotalRevenue = bookingValue,
                CollectedAmount = netCollected,
                PendingAmount = pending,
                TotalExpenses = expenseTotal,
                Profit = profit,
                Balance = balance,
                OverdueCount = bookings.Count(x => x.Status == BookingStatus.Overdue),
                DispatchedCount = bookings.Count(x => x.Status == BookingStatus.Dispatched)
            };

            TotalBookingsText = CurrentSummary.TotalBookings.ToString("N0");
            TotalRevenueText = $"₹{CurrentSummary.TotalRevenue:N0}";
            CollectedText = $"₹{CurrentSummary.CollectedAmount:N0}";
            PendingText = $"₹{CurrentSummary.PendingAmount:N0}";
            ExpensesText = $"₹{CurrentSummary.TotalExpenses:N0}";
            ProfitText = $"₹{CurrentSummary.Profit:N0}";
            BalanceText = $"₹{CurrentSummary.Balance:N0}";
            OverdueText = CurrentSummary.OverdueCount.ToString("N0");
            DispatchedText = CurrentSummary.DispatchedCount.ToString("N0");
            ReportPeriodText = $"{from:dd MMM yyyy}  →  {to:dd MMM yyyy}  ·  {ReportRows.Count} bookings · {ExpenseRows.Count} expenses";
            StatusMessage = ReportRows.Count == 0 && ExpenseRows.Count == 0
                ? "No records found for selected dates."
                : "Report ready. You can export PDF.";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task ExportPdfAsync()
    {
        if (!ValidationHelper.IsValidDateRange(FromDate, ToDate))
        {
            StatusMessage = "Generate a valid date range first.";
            return;
        }

        if (ReportRows.Count == 0 && ExpenseRows.Count == 0)
        {
            await GenerateReportAsync();
        }

        try
        {
            var company = await _settings.GetAsync();
            var path = _pdf.GenerateBusinessReport(
                company,
                FromDate!.Value.Date,
                ToDate!.Value.Date,
                CurrentSummary,
                ReportRows.ToList(),
                ExpenseRows.ToList());

            StatusMessage = "PDF exported successfully.";
            MessageBox.Show("Report PDF generated successfully.", "RentalX Report", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            StatusMessage = "PDF export failed.";
            MessageBox.Show(ex.Message, "PDF Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
