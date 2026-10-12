using Microsoft.EntityFrameworkCore;
using RentalX.Models;
using RentalX.Models.Enums;

namespace RentalX.Data.Repositories;

public class BookingRepository : IBookingRepository
{
    private readonly AppDbContext _db;

    public BookingRepository(AppDbContext db)
    {
        _db = db;
    }

    public Task<List<Booking>> GetAllAsync() =>
        _db.Bookings
            .AsNoTracking()
            .Include(x => x.Customer)
            .Include(x => x.InventoryItem)
            .Include(x => x.Lines)
            .OrderByDescending(x => x.BookingDate)
            .ThenByDescending(x => x.Id)
            .ToListAsync();

    public Task<List<Booking>> GetRecentAsync(int count = 10) =>
        _db.Bookings
            .AsNoTracking()
            .Include(x => x.Customer)
            .Include(x => x.InventoryItem)
            .Include(x => x.Lines)
            .OrderByDescending(x => x.BookingDate)
            .ThenByDescending(x => x.Id)
            .Take(count)
            .ToListAsync();

    public Task<List<Booking>> GetByStatusAsync(string status)
    {
        if (!Enum.TryParse<BookingStatus>(status, true, out var parsed))
        {
            return GetAllAsync();
        }

        return _db.Bookings
            .AsNoTracking()
            .Include(x => x.Customer)
            .Include(x => x.InventoryItem)
            .Include(x => x.Lines)
            .Where(x => x.Status == parsed)
            .OrderByDescending(x => x.BookingDate)
            .ToListAsync();
    }

    public Task<List<Booking>> GetByDateRangeAsync(DateTime fromDate, DateTime toDate)
    {
        var start = fromDate.Date;
        var end = toDate.Date.AddDays(1).AddTicks(-1);

        return _db.Bookings
            .AsNoTracking()
            .Include(x => x.Customer)
            .Include(x => x.InventoryItem)
            .Include(x => x.Lines)
            .Where(x => x.BookingDate >= start && x.BookingDate <= end)
            .OrderByDescending(x => x.BookingDate)
            .ThenByDescending(x => x.Id)
            .ToListAsync();
    }

    public Task<Booking?> GetByIdAsync(int id) =>
        _db.Bookings
            .Include(x => x.Customer)
            .Include(x => x.InventoryItem)
            .Include(x => x.Lines)
            .FirstOrDefaultAsync(x => x.Id == id);

    public async Task<string> GetNextBookingNumberAsync()
    {
        var last = await _db.Bookings
            .OrderByDescending(x => x.Id)
            .Select(x => x.BookingNumber)
            .FirstOrDefaultAsync();

        var next = 1000;
        if (!string.IsNullOrWhiteSpace(last))
        {
            var digits = new string(last.Where(char.IsDigit).ToArray());
            if (int.TryParse(digits, out var n))
            {
                next = n + 1;
            }
        }

        return $"BK-{next}";
    }

    public async Task<string> GetNextInvoiceNumberAsync()
    {
        var last = await _db.Bookings
            .Where(x => x.InvoiceNumber != null && x.InvoiceNumber != "")
            .OrderByDescending(x => x.Id)
            .Select(x => x.InvoiceNumber)
            .FirstOrDefaultAsync();

        var next = 5001;
        if (!string.IsNullOrWhiteSpace(last))
        {
            var digits = new string(last.Where(char.IsDigit).ToArray());
            if (int.TryParse(digits, out var n))
            {
                next = n + 1;
            }
        }

        return $"INV-{next}";
    }

    public async Task<Booking> AddAsync(Booking booking)
    {
        _db.Bookings.Add(booking);
        await _db.SaveChangesAsync();
        return booking;
    }

    public async Task UpdateAsync(Booking booking)
    {
        _db.Bookings.Update(booking);
        await _db.SaveChangesAsync();
    }

    public async Task UpdateStatusAsync(int bookingId, BookingStatus status)
    {
        var booking = await _db.Bookings.FirstOrDefaultAsync(x => x.Id == bookingId);
        if (booking is null)
        {
            return;
        }

        booking.Status = status;
        await _db.SaveChangesAsync();
    }

    public async Task UpdateAmountAsync(int bookingId, decimal amount)
    {
        var booking = await _db.Bookings.FirstOrDefaultAsync(x => x.Id == bookingId);
        if (booking is null)
        {
            return;
        }

        booking.Amount = amount;
        await _db.SaveChangesAsync();
    }
}
