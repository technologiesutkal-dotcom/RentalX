using RentalX.Models;

namespace RentalX.Data.Repositories;

public interface IPaymentRepository
{
    Task<List<Payment>> GetAllAsync();
    Task<decimal> GetCollectedTodayAsync();
    Task<decimal> GetRefundedTodayAsync();
    Task<decimal> GetPendingAsync();
    Task<decimal> GetDepositsHeldAsync();
    Task<List<Payment>> GetByDateRangeAsync(DateTime fromDate, DateTime toDate);
    Task AddAsync(Payment payment);
    Task<bool> HasCollectedPaymentAsync(int bookingId);
}
