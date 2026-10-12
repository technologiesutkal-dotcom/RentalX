namespace RentalX.Models;

public class BookingLine
{
    public int Id { get; set; }
    public int BookingId { get; set; }
    public Booking? Booking { get; set; }
    public int InventoryItemId { get; set; }
    public InventoryItem? InventoryItem { get; set; }
    public string ItemName { get; set; } = string.Empty;
    public int Quantity { get; set; } = 1;
    public decimal DailyRate { get; set; }
    /// <summary>DailyRate × Quantity × rental days at booking time.</summary>
    public decimal LineAmount { get; set; }

    public string LineText => $"{Quantity} × {ItemName} @ ₹{DailyRate:N0}/day = ₹{LineAmount:N0}";
}

/// <summary>UI cart row before booking is saved.</summary>
public class BookingCartItem
{
    public int InventoryItemId { get; set; }
    public string ItemName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal DailyRate { get; set; }
    public decimal LineTotal { get; set; }
    public string LineText => $"{Quantity} × {ItemName} @ ₹{DailyRate:N0}/day";
    public string AmountText => $"₹{LineTotal:N0}";
}
