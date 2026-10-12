using Microsoft.EntityFrameworkCore;
using RentalX.Models;
using RentalX.Models.Enums;

namespace RentalX.Data.Repositories;

public class DashboardRepository : IDashboardRepository
{
    private readonly AppDbContext _db;

    public DashboardRepository(AppDbContext db)
    {
        _db = db;
    }

    public async Task<DashboardSummary> GetSummaryAsync()
    {
        var inventory = await _db.InventoryItems.AsNoTracking().ToListAsync();
        var activeStatuses = new[]
        {
            BookingStatus.Confirmed,
            BookingStatus.Dispatched,
            BookingStatus.Overdue
        };

        var today = DateTime.Today;
        var tomorrow = today.AddDays(1);

        // SQLite cannot Sum(decimal) in SQL — load values then aggregate in memory.
        // Money in today = advances + balance + late settlement (all Collected).
        var collected = await SumPaymentsAsync(x =>
            x.Status == PaymentStatus.Collected &&
            x.PaymentDate >= today &&
            x.PaymentDate < tomorrow);

        var refunded = await SumPaymentsAsync(x =>
            x.Status == PaymentStatus.Refunded &&
            x.PaymentDate >= today &&
            x.PaymentDate < tomorrow);

        var pending = await SumPaymentsAsync(x =>
            x.Status == PaymentStatus.Pending &&
            !x.IsDeposit);

        return new DashboardSummary
        {
            TotalItems = inventory.Sum(x => x.TotalQuantity),
            AvailableItems = inventory.Sum(x => x.AvailableQuantity),
            ActiveRentals = await _db.Bookings.CountAsync(x => activeStatuses.Contains(x.Status)),
            OverdueCount = await _db.Bookings.CountAsync(x => x.Status == BookingStatus.Overdue),
            TodaysRevenue = collected - refunded,
            RevenueGrowthPercent = 12.5m,
            CollectedToday = collected,
            PendingAmount = pending,
            DepositsHeld = refunded,
            RefundedToday = refunded
        };
    }

    public async Task<List<UpcomingReturnItem>> GetUpcomingReturnsAsync()
    {
        var today = DateTime.Today;
        var bookings = await _db.Bookings
            .AsNoTracking()
            .Include(x => x.Customer)
            .Where(x => x.ReturnDate != null &&
                        x.Status != BookingStatus.Returned &&
                        x.Status != BookingStatus.Cancelled)
            .OrderBy(x => x.ReturnDate)
            .Take(5)
            .ToListAsync();

        return bookings.Select(x =>
        {
            var returnDate = x.ReturnDate!.Value.Date;
            var isToday = returnDate == today;
            return new UpcomingReturnItem
            {
                ItemName = x.ItemsSummary,
                CustomerName = x.Customer?.Name ?? "Unknown",
                DueText = isToday
                    ? "Today 5:30 PM"
                    : returnDate == today.AddDays(1)
                        ? "Tomorrow"
                        : returnDate.ToString("dd MMM"),
                IsToday = isToday
            };
        }).ToList();
    }

    public async Task<List<LowStockItem>> GetLowStockAsync()
    {
        var items = await _db.InventoryItems.AsNoTracking().ToListAsync();
        return items
            .Where(x => x.AvailableQuantity <= 15)
            .OrderBy(x => x.AvailableQuantity)
            .Select(x => new LowStockItem
            {
                Name = x.Name,
                Available = x.AvailableQuantity,
                IsCritical = x.AvailableQuantity <= 5
            })
            .ToList();
    }

    private async Task<decimal> SumPaymentsAsync(System.Linq.Expressions.Expression<Func<Payment, bool>> predicate)
    {
        var amounts = await _db.Payments
            .AsNoTracking()
            .Where(predicate)
            .Select(x => x.Amount)
            .ToListAsync();

        return amounts.Count == 0 ? 0m : amounts.Sum();
    }
}
