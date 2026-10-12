namespace RentalX.LicenseApi.Models;

public class SerialRegistration
{
    public int Id { get; set; }
    public string SerialNumber { get; set; } = string.Empty;
    public string? MachineName { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public string ContactName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string? Address { get; set; }
    public string? Pincode { get; set; }
    public string? Notes { get; set; }
    public string Status { get; set; } = SerialStatuses.Pending;
    public int? ClientId { get; set; }
    public Client? Client { get; set; }
    public int? LicenseId { get; set; }
    public License? License { get; set; }
    public DateTime AppliedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ReviewedAt { get; set; }
    public string? ReviewNotes { get; set; }
}

public static class SerialStatuses
{
    public const string Pending = "Pending";
    public const string Approved = "Approved";
    public const string Rejected = "Rejected";
    public const string Revoked = "Revoked";
    public const string Deactivated = "Deactivated";
}
