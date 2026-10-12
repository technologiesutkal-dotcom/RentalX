using Microsoft.EntityFrameworkCore;
using RentalX.Models;

namespace RentalX.Data.Repositories;

public class UserRepository : IUserRepository
{
    private readonly AppDbContext _db;

    public UserRepository(AppDbContext db)
    {
        _db = db;
    }

    public Task<List<AppUser>> GetAllAsync() =>
        _db.Users.AsNoTracking().OrderBy(x => x.FullName).ToListAsync();

    public Task<AppUser?> GetAdminAsync() =>
        _db.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Role == "Administrator");

    public async Task<bool> EmailExistsAsync(string email, int? excludeId = null)
    {
        var normalized = email.Trim().ToLowerInvariant();
        var users = await _db.Users.AsNoTracking().ToListAsync();
        return users.Any(x =>
            string.Equals(x.Email, normalized, StringComparison.OrdinalIgnoreCase) &&
            (!excludeId.HasValue || x.Id != excludeId.Value));
    }

    public async Task<bool> PhoneExistsAsync(string phone, int? excludeId = null)
    {
        var digits = new string(phone.Where(char.IsDigit).ToArray());
        var users = await _db.Users.AsNoTracking().ToListAsync();
        return users.Any(x =>
            new string(x.Phone.Where(char.IsDigit).ToArray()) == digits &&
            (!excludeId.HasValue || x.Id != excludeId.Value));
    }

    public async Task AddAsync(AppUser user)
    {
        _db.Users.Add(user);
        await _db.SaveChangesAsync();
    }

    public async Task UpdateAsync(AppUser user)
    {
        _db.Users.Update(user);
        await _db.SaveChangesAsync();
    }

    public async Task SetActiveAsync(int id, bool isActive)
    {
        var user = await _db.Users.FirstOrDefaultAsync(x => x.Id == id);
        if (user is null)
        {
            return;
        }

        user.IsActive = isActive;
        await _db.SaveChangesAsync();
    }
}
