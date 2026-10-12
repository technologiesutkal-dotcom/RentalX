namespace RentalX.Models;

public class AppNotification
{
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Type { get; set; } = "Info";
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public string TimeText => CreatedAt.ToString("dd MMM, hh:mm tt");
}
