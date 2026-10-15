namespace RentalX.Services;

public interface IPlatformSession
{
    string? Token { get; }
    string? Role { get; }
    string? DisplayName { get; }
    string? Username { get; }
    int? ClientId { get; }
    string? CompanyName { get; }
    bool IsSuperAdmin { get; }
    bool IsClient { get; }

    void Set(
        string token,
        string role,
        string displayName,
        int? clientId = null,
        string? companyName = null,
        string? username = null);
    void Clear();
}
