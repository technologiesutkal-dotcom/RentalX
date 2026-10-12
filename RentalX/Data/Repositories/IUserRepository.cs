using RentalX.Models;

namespace RentalX.Data.Repositories;

public interface IUserRepository
{
    Task<List<AppUser>> GetAllAsync();
    Task<AppUser?> GetAdminAsync();
    Task<bool> EmailExistsAsync(string email, int? excludeId = null);
    Task<bool> PhoneExistsAsync(string phone, int? excludeId = null);
    Task AddAsync(AppUser user);
    Task UpdateAsync(AppUser user);
    Task SetActiveAsync(int id, bool isActive);
}
