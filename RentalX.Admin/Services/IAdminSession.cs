namespace RentalX.Admin.Services;

public interface IAdminSession
{
    string? Token { get; }
    string? Username { get; }
    string? DisplayName { get; }
    string? Role { get; }
    bool IsAuthenticated { get; }
    event Action? SignedOut;

    void SetSession(string token, string displayName, string? role = null, string? username = null);
    void UpdateUsername(string username);
    void Clear(bool raiseSignedOut = true);
}
