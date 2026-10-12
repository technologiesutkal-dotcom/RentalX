namespace RentalX.LicenseApi.Models;

public class License
{
    public int Id { get; set; }
    public int ClientId { get; set; }
    public Client? Client { get; set; }
    public string LicenseKey { get; set; } = string.Empty;
    public string PlanType { get; set; } = PlanTypes.Monthly;
    public int MaxMachines { get; set; } = 1;
    public DateTime StartsAt { get; set; } = DateTime.UtcNow.Date;
    public DateTime ExpiresAt { get; set; }
    public string Status { get; set; } = LicenseStatuses.Active;
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public ICollection<Activation> Activations { get; set; } = new List<Activation>();
    public ICollection<Payment> Payments { get; set; } = new List<Payment>();
}

public static class PlanTypes
{
    public const string Trial = "Trial";
    public const string Monthly = "Monthly";
    public const string Yearly = "Yearly";
    public const string Lifetime = "Lifetime";
}

public static class LicenseStatuses
{
    public const string Active = "Active";
    public const string Expired = "Expired";
    public const string Revoked = "Revoked";
    public const string Suspended = "Suspended";
}
