using RentalX.Data.Repositories;
using RentalX.Helpers;
using RentalX.Models;

namespace RentalX.Services;

public class AuthService : IAuthService
{
    public const string DefaultUsernameValue = "admin";
    public const string DefaultPasswordValue = "RentalX@123";

    private readonly ILoginAccountRepository _accounts;

    public AuthService(ILoginAccountRepository accounts)
    {
        _accounts = accounts;
    }

    public string DefaultUsername => DefaultUsernameValue;
    public string DefaultPasswordHint => DefaultPasswordValue;

    public async Task EnsureDefaultAccountAsync()
    {
        if (await _accounts.AnyAsync())
        {
            return;
        }

        var (hash, salt) = PasswordHasher.HashPassword(DefaultPasswordValue);
        await _accounts.AddAsync(new LoginAccount
        {
            Username = DefaultUsernameValue,
            PasswordHash = hash,
            PasswordSalt = salt,
            DisplayName = "Administrator",
            MustChangePassword = true,
            UpdatedAt = DateTime.Now
        });
    }

    public async Task<LoginAccount?> ValidateLoginAsync(string username, string password)
    {
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            return null;
        }

        var account = await _accounts.GetByUsernameAsync(username);
        if (account is null)
        {
            return null;
        }

        return PasswordHasher.Verify(password, account.PasswordHash, account.PasswordSalt)
            ? account
            : null;
    }

    public Task<LoginAccount?> GetAccountAsync() => _accounts.GetAsync();

    public async Task ChangeCredentialsAsync(string currentPassword, string newUsername, string newPassword)
    {
        var account = await _accounts.GetAsync()
            ?? throw new InvalidOperationException("Login account not found.");

        if (!PasswordHasher.Verify(currentPassword, account.PasswordHash, account.PasswordSalt))
        {
            throw new InvalidOperationException("Current password is incorrect.");
        }

        if (string.IsNullOrWhiteSpace(newUsername) || newUsername.Trim().Length < 3)
        {
            throw new InvalidOperationException("Login ID must be at least 3 characters.");
        }

        if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < 6)
        {
            throw new InvalidOperationException("New password must be at least 6 characters.");
        }

        var normalized = newUsername.Trim().ToLowerInvariant();
        var existing = await _accounts.GetByUsernameAsync(normalized);
        if (existing is not null && existing.Id != account.Id)
        {
            throw new InvalidOperationException("That login ID is already in use.");
        }

        var (hash, salt) = PasswordHasher.HashPassword(newPassword);
        account.Username = normalized;
        account.PasswordHash = hash;
        account.PasswordSalt = salt;
        account.MustChangePassword = false;
        await _accounts.UpdateAsync(account);
    }
}
