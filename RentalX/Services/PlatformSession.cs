using RentalX.Admin.Models;

namespace RentalX.Services;

public sealed class PlatformSession : IPlatformSession
{
    public string? Token { get; private set; }
    public string? Role { get; private set; }
    public string? DisplayName { get; private set; }
    public int? ClientId { get; private set; }
    public string? CompanyName { get; private set; }

    public bool IsSuperAdmin =>
        string.Equals(Role, UserRoles.SuperAdmin, StringComparison.OrdinalIgnoreCase);

    public bool IsClient =>
        string.Equals(Role, UserRoles.Client, StringComparison.OrdinalIgnoreCase);

    public void Set(string token, string role, string displayName, int? clientId = null, string? companyName = null)
    {
        Token = token;
        Role = role;
        DisplayName = displayName;
        ClientId = clientId;
        CompanyName = companyName;
    }

    public void Clear()
    {
        Token = null;
        Role = null;
        DisplayName = null;
        ClientId = null;
        CompanyName = null;
    }
}
