using RentalX.Models.Enums;

namespace RentalX.Models;

public class Booking
{
    public int Id { get; set; }
    public string BookingNumber { get; set; } = string.Empty;
    public int CustomerId { get; set; }
    public Customer? Customer { get; set; }
    public int? InventoryItemId { get; set; }
    public InventoryItem? InventoryItem { get; set; }
    public string ItemsSummary { get; set; } = string.Empty;
    public int Quantity { get; set; } = 1;
    public int RentalDays { get; set; } = 1;
    public decimal DailyRate { get; set; }
    public ICollection<BookingLine> Lines { get; set; } = new List<BookingLine>();

    /// <summary>Charge for one rental day across all lines (or legacy single item).</summary>
    public decimal PerDayCharge =>
        Lines is { Count: > 0 }
            ? Lines.Sum(l => l.DailyRate * l.Quantity)
            : DailyRate * Quantity;
    public DateTime BookingDate { get; set; } = DateTime.Today;
    /// <summary>Planned return date agreed at booking.</summary>
    public DateTime? ReturnDate { get; set; }
    /// <summary>Same as planned return; kept for clarity in settlement.</summary>
    public DateTime? ExpectedReturnDate { get; set; }
    /// <summary>Date items were actually received back.</summary>
    public DateTime? ActualReturnDate { get; set; }
    /// <summary>Amount at booking time (before late/early settlement).</summary>
    public decimal OriginalAmount { get; set; }
    /// <summary>Final payable after settlement (late charges / early credit / manual).</summary>
    public decimal Amount { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal AdjustmentAmount { get; set; }
    public int LateDays { get; set; }
    public int EarlyDays { get; set; }
    public string? SettlementNote { get; set; }
    public BookingStatus Status { get; set; } = BookingStatus.Pending;
    public string? InvoiceNumber { get; set; }
    public bool IsInvoiced { get; set; }

    public string CustomerName => Customer?.Name ?? string.Empty;
    public string StatusText => Status switch
    {
        BookingStatus.Confirmed => "Booked (waiting dispatch)",
        BookingStatus.Dispatched => IsOverdueToday ? "Overdue (still out)" : "Out with customer",
        BookingStatus.Returned => "Returned / Closed",
        BookingStatus.Overdue => "Overdue (still out)",
        BookingStatus.Cancelled => "Cancelled",
        _ => Status.ToString()
    };
    public string AmountText => $"₹{Amount:N0}";
    public string PaidText => $"₹{PaidAmount:N0}";
    public string BalanceText => $"₹{BalanceDue:N0}";
    public decimal BalanceDue => Math.Max(0, Amount - PaidAmount);
    public decimal RefundDue => Math.Max(0, PaidAmount - Amount);
    public bool IsFullyPaid => BalanceDue <= 0 && RefundDue <= 0;
    public string PaymentStatusText =>
        RefundDue > 0 ? "Refund due" :
        BalanceDue <= 0 ? "Fully paid" :
        PaidAmount > 0 ? "Advance paid" : "Unpaid";
    public string BookingDateText => BookingDate.ToString("dd MMM yyyy");
    public string ExpectedReturnText => (ExpectedReturnDate ?? ReturnDate)?.ToString("dd MMM yyyy") ?? "-";
    public string ActualReturnText => ActualReturnDate?.ToString("dd MMM yyyy") ?? "-";
    public string InvoiceText => string.IsNullOrWhiteSpace(InvoiceNumber) ? "-" : InvoiceNumber;
    public decimal CalculatedAmount => PerDayCharge * Math.Max(1, RentalDays);
    public bool IsOverdueToday
    {
        get
        {
            if (Status is not (BookingStatus.Dispatched or BookingStatus.Overdue))
            {
                return false;
            }

            var expected = ExpectedReturnDate ?? ReturnDate;
            return expected.HasValue && DateTime.Today > expected.Value.Date;
        }
    }
}
