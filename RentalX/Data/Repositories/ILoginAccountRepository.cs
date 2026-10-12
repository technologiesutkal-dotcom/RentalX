using RentalX.Models;

namespace RentalX.Data.Repositories;

public interface ILoginAccountRepository
{
    Task<LoginAccount?> GetAsync();
    Task<LoginAccount?> GetByUsernameAsync(string username);
    Task AddAsync(LoginAccount account);
    Task UpdateAsync(LoginAccount account);
    Task<bool> AnyAsync();
}
