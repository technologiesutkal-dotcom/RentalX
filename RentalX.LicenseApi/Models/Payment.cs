namespace RentalX.LicenseApi.Models;

public class Payment
{
    public int Id { get; set; }
    public int ClientId { get; set; }
    public Client? Client { get; set; }
    public int? LicenseId { get; set; }
    public License? License { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "INR";
    public string Method { get; set; } = PaymentMethods.Manual;
    public string? Reference { get; set; }
    public string Status { get; set; } = PaymentStatuses.Completed;
    public DateTime PaidAt { get; set; } = DateTime.UtcNow;
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public static class PaymentMethods
{
    public const string Manual = "Manual";
    public const string Upi = "UPI";
    public const string BankTransfer = "BankTransfer";
    public const string Razorpay = "Razorpay";
    public const string Cash = "Cash";
}

public static class PaymentStatuses
{
    public const string Pending = "Pending";
    public const string Completed = "Completed";
    public const string Failed = "Failed";
    public const string Refunded = "Refunded";
}
