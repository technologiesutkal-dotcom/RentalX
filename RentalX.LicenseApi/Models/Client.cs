namespace RentalX.LicenseApi.Models;

public class Client
{
    public int Id { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public string ContactName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string? City { get; set; }
    public string? Address { get; set; }
    public string? Notes { get; set; }
    public string Status { get; set; } = ClientStatuses.Active;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public ICollection<License> Licenses { get; set; } = new List<License>();
    public ICollection<Payment> Payments { get; set; } = new List<Payment>();
}

public static class ClientStatuses
{
    public const string Active = "Active";
    public const string Suspended = "Suspended";
    public const string Inactive = "Inactive";
    public const string Deactivated = "Deactivated";
}
