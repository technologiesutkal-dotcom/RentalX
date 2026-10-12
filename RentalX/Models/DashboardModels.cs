namespace RentalX.Models;

public class DashboardSummary
{
    public int TotalItems { get; set; }
    public int AvailableItems { get; set; }
    public int ActiveRentals { get; set; }
    public int OverdueCount { get; set; }
    public decimal TodaysRevenue { get; set; }
    public decimal RevenueGrowthPercent { get; set; }
    public decimal CollectedToday { get; set; }
    public decimal PendingAmount { get; set; }
    /// <summary>Legacy alias used by UI — now holds Refunded Today amount.</summary>
    public decimal DepositsHeld { get; set; }
    public decimal RefundedToday { get; set; }
}

public class UpcomingReturnItem
{
    public string ItemName { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string DueText { get; set; } = string.Empty;
    public bool IsToday { get; set; }
}

public class LowStockItem
{
    public string Name { get; set; } = string.Empty;
    public int Available { get; set; }
    public bool IsCritical { get; set; }
}
