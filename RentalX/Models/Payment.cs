using RentalX.Models.Enums;

namespace RentalX.Models;

public class Payment
{
    public int Id { get; set; }
    public int? BookingId { get; set; }
    public Booking? Booking { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public PaymentStatus Status { get; set; } = PaymentStatus.Pending;
    public DateTime PaymentDate { get; set; } = DateTime.Today;
    public bool IsDeposit { get; set; }

    public string AmountText => $"₹{Amount:N0}";
    public string StatusText => Status.ToString();
}
