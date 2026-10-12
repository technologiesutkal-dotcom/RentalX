using Microsoft.EntityFrameworkCore;
using RentalX.Models;
using RentalX.Models.Enums;

namespace RentalX.Data.Repositories;

public class PaymentRepository : IPaymentRepository
{
    private readonly AppDbContext _db;

    public PaymentRepository(AppDbContext db)
    {
        _db = db;
    }

    public Task<List<Payment>> GetAllAsync() =>
        _db.Payments
            .AsNoTracking()
            .Include(x => x.Booking)
            .OrderByDescending(x => x.PaymentDate)
            .ToListAsync();

    /// <summary>All money received today: advance + balance + late settlement.</summary>
    public async Task<decimal> GetCollectedTodayAsync()
    {
        var today = DateTime.Today;
        var tomorrow = today.AddDays(1);
        return await SumAmountsAsync(x =>
            x.Status == PaymentStatus.Collected &&
            x.PaymentDate >= today &&
            x.PaymentDate < tomorrow);
    }

    /// <summary>Early-return / other refunds paid out today.</summary>
    public async Task<decimal> GetRefundedTodayAsync()
    {
        var today = DateTime.Today;
        var tomorrow = today.AddDays(1);
        return await SumAmountsAsync(x =>
            x.Status == PaymentStatus.Refunded &&
            x.PaymentDate >= today &&
            x.PaymentDate < tomorrow);
    }

    public Task<decimal> GetPendingAsync() =>
        SumAmountsAsync(x => x.Status == PaymentStatus.Pending && !x.IsDeposit);

    /// <summary>Advances still held on open (not returned/cancelled) bookings.</summary>
    public async Task<decimal> GetDepositsHeldAsync()
    {
        var openStatuses = new[]
        {
            BookingStatus.Confirmed,
            BookingStatus.Dispatched,
            BookingStatus.Overdue
        };

        var amounts = await _db.Payments
            .AsNoTracking()
            .Include(x => x.Booking)
            .Where(x =>
                x.IsDeposit &&
                x.Status == PaymentStatus.Collected &&
                x.Booking != null &&
                openStatuses.Contains(x.Booking.Status))
            .Select(x => x.Amount)
            .ToListAsync();

        return amounts.Count == 0 ? 0m : amounts.Sum();
    }

    public Task<List<Payment>> GetByDateRangeAsync(DateTime fromDate, DateTime toDate)
    {
        var start = fromDate.Date;
        var end = toDate.Date.AddDays(1).AddTicks(-1);

        return _db.Payments
            .AsNoTracking()
            .Include(x => x.Booking)
            .Where(x => x.PaymentDate >= start && x.PaymentDate <= end)
            .OrderByDescending(x => x.PaymentDate)
            .ToListAsync();
    }

    public async Task AddAsync(Payment payment)
    {
        _db.Payments.Add(payment);
        await _db.SaveChangesAsync();
    }

    public Task<bool> HasCollectedPaymentAsync(int bookingId) =>
        _db.Payments.AsNoTracking().AnyAsync(x =>
            x.BookingId == bookingId &&
            x.Status == PaymentStatus.Collected &&
            !x.IsDeposit);

    private async Task<decimal> SumAmountsAsync(System.Linq.Expressions.Expression<Func<Payment, bool>> predicate)
    {
        // SQLite does not support SUM on decimal in SQL translation.
        var amounts = await _db.Payments
            .AsNoTracking()
            .Where(predicate)
            .Select(x => x.Amount)
            .ToListAsync();

        return amounts.Count == 0 ? 0m : amounts.Sum();
    }
}
