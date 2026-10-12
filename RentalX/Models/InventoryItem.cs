namespace RentalX.Models;

public class InventoryItem
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public int TotalQuantity { get; set; }
    public int RentedQuantity { get; set; }
    public decimal DailyRate { get; set; }
    public string Status { get; set; } = "Active";

    public int AvailableQuantity => TotalQuantity - RentedQuantity;
    public double AvailablePercent => TotalQuantity == 0 ? 0 : (AvailableQuantity * 100.0) / TotalQuantity;
}
