namespace RentalX.LicenseApi.Models;

public class ClientSession
{
    public int Id { get; set; }
    public int ClientUserId { get; set; }
    public ClientUser? ClientUser { get; set; }
    public string Token { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime ExpiresAt { get; set; }
    public DateTime? RevokedAt { get; set; }
}
