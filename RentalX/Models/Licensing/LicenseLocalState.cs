namespace RentalX.Models.Licensing;

public sealed class LicenseLocalState
{
    public string LicenseKey { get; set; } = string.Empty;
    public string MachineId { get; set; } = string.Empty;
    public string? CompanyName { get; set; }
    public string? PlanType { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public DateTime ActivatedAt { get; set; } = DateTime.UtcNow;
    public DateTime LastValidatedAt { get; set; } = DateTime.UtcNow;
}
