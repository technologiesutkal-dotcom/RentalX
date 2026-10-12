namespace RentalX.Models;

public class NavigationItem
{
    public string Key { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
    public string Group { get; set; } = string.Empty;
    public bool IsSelected { get; set; }
}
