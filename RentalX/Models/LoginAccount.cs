namespace RentalX.Models;

public class LoginAccount
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string PasswordSalt { get; set; } = string.Empty;
    public string DisplayName { get; set; } = "Administrator";
    public bool MustChangePassword { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.Now;
}
