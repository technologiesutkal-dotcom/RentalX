using Microsoft.EntityFrameworkCore;
using RentalX.Models;

namespace RentalX.Data.Repositories;

public class LoginAccountRepository : ILoginAccountRepository
{
    private readonly AppDbContext _db;

    public LoginAccountRepository(AppDbContext db)
    {
        _db = db;
    }

    public Task<LoginAccount?> GetAsync() =>
        _db.LoginAccounts.OrderBy(x => x.Id).FirstOrDefaultAsync();

    public Task<LoginAccount?> GetByUsernameAsync(string username)
    {
        var normalized = username.Trim().ToLowerInvariant();
        return _db.LoginAccounts.FirstOrDefaultAsync(x => x.Username == normalized);
    }

    public Task<bool> AnyAsync() => _db.LoginAccounts.AnyAsync();

    public async Task AddAsync(LoginAccount account)
    {
        _db.LoginAccounts.Add(account);
        await _db.SaveChangesAsync();
    }

    public async Task UpdateAsync(LoginAccount account)
    {
        account.UpdatedAt = DateTime.Now;
        _db.LoginAccounts.Update(account);
        await _db.SaveChangesAsync();
    }
}
