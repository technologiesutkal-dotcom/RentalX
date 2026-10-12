namespace RentalX.LicenseApi.Models;

/// <summary>
/// Laptops allowed to sign in as Super Admin (same username/password, multiple PCs).
/// </summary>
public class AdminSerial
{
    public int Id { get; set; }
    public string SerialNumber { get; set; } = string.Empty;
    public string? MachineName { get; set; }
    public string? Notes { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastLoginAt { get; set; }
}
