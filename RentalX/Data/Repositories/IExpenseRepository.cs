using RentalX.Models;

namespace RentalX.Data.Repositories;

public interface IExpenseRepository
{
    Task<List<Expense>> GetAllAsync();
    Task<List<Expense>> GetByDateRangeAsync(DateTime fromDate, DateTime toDate);
    Task AddAsync(Expense expense);
    Task<decimal> GetTotalByDateRangeAsync(DateTime fromDate, DateTime toDate);
}
