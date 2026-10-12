using RentalX.Models;
using RentalX.Models.Enums;

namespace RentalX.Data.Repositories;

public interface IBookingRepository
{
    Task<List<Booking>> GetAllAsync();
    Task<List<Booking>> GetRecentAsync(int count = 10);
    Task<List<Booking>> GetByStatusAsync(string status);
    Task<List<Booking>> GetByDateRangeAsync(DateTime fromDate, DateTime toDate);
    Task<Booking?> GetByIdAsync(int id);
    Task<string> GetNextBookingNumberAsync();
    Task<string> GetNextInvoiceNumberAsync();
    Task<Booking> AddAsync(Booking booking);
    Task UpdateAsync(Booking booking);
    Task UpdateStatusAsync(int bookingId, BookingStatus status);
    Task UpdateAmountAsync(int bookingId, decimal amount);
}
