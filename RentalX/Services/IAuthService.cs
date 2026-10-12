using RentalX.Models;

namespace RentalX.Services;

public interface IAuthService
{
    Task EnsureDefaultAccountAsync();
    Task<LoginAccount?> ValidateLoginAsync(string username, string password);
    Task<LoginAccount?> GetAccountAsync();
    Task ChangeCredentialsAsync(string currentPassword, string newUsername, string newPassword);
    string DefaultUsername { get; }
    string DefaultPasswordHint { get; }
}
