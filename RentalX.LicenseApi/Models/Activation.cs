namespace RentalX.LicenseApi.Models;

public class Activation
{
    public int Id { get; set; }
    public int LicenseId { get; set; }
    public License? License { get; set; }
    public string MachineId { get; set; } = string.Empty;
    public string? MachineName { get; set; }
    public string? OsInfo { get; set; }
    public DateTime ActivatedAt { get; set; } = DateTime.UtcNow;
    public DateTime LastSeenAt { get; set; } = DateTime.UtcNow;
    public bool IsActive { get; set; } = true;
    public DateTime? DeactivatedAt { get; set; }
    public string? DeactivateReason { get; set; }
}
