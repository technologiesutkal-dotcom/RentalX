namespace RentalX.LicenseApi.Models;

public class AdminSession
{
    public int Id { get; set; }
    public int AdminUserId { get; set; }
    public AdminUser? AdminUser { get; set; }
    public string Token { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime ExpiresAt { get; set; }
    public DateTime? RevokedAt { get; set; }
}
