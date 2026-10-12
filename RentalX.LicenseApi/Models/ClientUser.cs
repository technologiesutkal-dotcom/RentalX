namespace RentalX.LicenseApi.Models;

public class ClientUser
{
    public int Id { get; set; }
    public int ClientId { get; set; }
    public Client? Client { get; set; }
    public string Username { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string PasswordSalt { get; set; } = string.Empty;

    /// <summary>Plain password for Super Admin display only (always reset on approve/reactivate).</summary>
    public string PortalPassword { get; set; } = Helpers.PortalCredentials.DefaultPassword;

    public string DisplayName { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public static class UserRoles
{
    public const string SuperAdmin = "SuperAdmin";
    public const string Client = "Client";
}
