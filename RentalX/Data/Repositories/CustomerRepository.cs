using Microsoft.EntityFrameworkCore;
using RentalX.Models;

namespace RentalX.Data.Repositories;

public class CustomerRepository : ICustomerRepository
{
    private readonly AppDbContext _db;

    public CustomerRepository(AppDbContext db)
    {
        _db = db;
    }

    public Task<List<Customer>> GetAllAsync() =>
        _db.Customers.AsNoTracking().OrderBy(x => x.Name).ToListAsync();

    public Task<Customer?> GetByIdAsync(int id) =>
        _db.Customers.FirstOrDefaultAsync(x => x.Id == id);

    public async Task AddAsync(Customer customer)
    {
        _db.Customers.Add(customer);
        await _db.SaveChangesAsync();
    }

    public async Task UpdateAsync(Customer customer)
    {
        _db.Customers.Update(customer);
        await _db.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var customer = await _db.Customers.FindAsync(id);
        if (customer is null)
        {
            return;
        }

        _db.Customers.Remove(customer);
        await _db.SaveChangesAsync();
    }
}
