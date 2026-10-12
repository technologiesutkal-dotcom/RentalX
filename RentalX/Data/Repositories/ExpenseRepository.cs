using Microsoft.EntityFrameworkCore;
using RentalX.Models;

namespace RentalX.Data.Repositories;

public class ExpenseRepository : IExpenseRepository
{
    private readonly AppDbContext _db;

    public ExpenseRepository(AppDbContext db)
    {
        _db = db;
    }

    public Task<List<Expense>> GetAllAsync() =>
        _db.Expenses.AsNoTracking().OrderByDescending(x => x.ExpenseDate).ToListAsync();

    public Task<List<Expense>> GetByDateRangeAsync(DateTime fromDate, DateTime toDate)
    {
        var start = fromDate.Date;
        var end = toDate.Date.AddDays(1).AddTicks(-1);
        return _db.Expenses
            .AsNoTracking()
            .Where(x => x.ExpenseDate >= start && x.ExpenseDate <= end)
            .OrderByDescending(x => x.ExpenseDate)
            .ToListAsync();
    }

    public async Task AddAsync(Expense expense)
    {
        _db.Expenses.Add(expense);
        await _db.SaveChangesAsync();
    }

    public async Task<decimal> GetTotalByDateRangeAsync(DateTime fromDate, DateTime toDate)
    {
        var items = await GetByDateRangeAsync(fromDate, toDate);
        return items.Count == 0 ? 0m : items.Sum(x => x.Amount);
    }
}
