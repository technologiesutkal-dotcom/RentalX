namespace RentalX.Models;

public class ReportSummary
{
    public int TotalBookings { get; set; }
    public decimal TotalRevenue { get; set; }
    public decimal CollectedAmount { get; set; }
    public decimal PendingAmount { get; set; }
    public decimal TotalExpenses { get; set; }
    public decimal Profit { get; set; }
    public decimal Balance { get; set; }
    public int OverdueCount { get; set; }
    public int DispatchedCount { get; set; }
}

public class ReportBookingRow
{
    public string BookingNumber { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string ItemsSummary { get; set; } = string.Empty;
    public DateTime BookingDate { get; set; }
    public decimal Amount { get; set; }
    public string StatusText { get; set; } = string.Empty;

    public string BookingDateText => BookingDate.ToString("dd MMM yyyy");
    public string AmountText => $"₹{Amount:N0}";
}

public class SearchResultItem
{
    public string Category { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Subtitle { get; set; } = string.Empty;
    public string PageKey { get; set; } = string.Empty;
}
