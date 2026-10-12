namespace RentalX.Models;

public class ReturnSettlement
{
    public int BookingId { get; set; }
    public string BookingNumber { get; set; } = string.Empty;
    public DateTime ExpectedReturnDate { get; set; }
    public DateTime ActualReturnDate { get; set; }
    public int PlannedDays { get; set; }
    public int ActualDays { get; set; }
    public int LateDays { get; set; }
    public int EarlyDays { get; set; }
    public string SettlementType { get; set; } = "OnTime";
    public decimal DailyRate { get; set; }
    public int Quantity { get; set; }
    public decimal OriginalAmount { get; set; }
    public decimal AutoAdjustment { get; set; }
    public decimal ManualAdjustment { get; set; }
    public decimal FinalAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal AmountToCollect { get; set; }
    public decimal RefundDue { get; set; }
    public bool ApplyEarlyCredit { get; set; } = true;
    public string Summary { get; set; } = string.Empty;

    public string OriginalAmountText => $"₹{OriginalAmount:N0}";
    public string AutoAdjustmentText => AutoAdjustment >= 0 ? $"+₹{AutoAdjustment:N0}" : $"-₹{Math.Abs(AutoAdjustment):N0}";
    public string FinalAmountText => $"₹{FinalAmount:N0}";
    public string AmountToCollectText => $"₹{AmountToCollect:N0}";
    public string RefundDueText => $"₹{RefundDue:N0}";
    public string TypeLabel => SettlementType switch
    {
        "Late" => $"Late by {LateDays} day(s)",
        "Early" => $"Early by {EarlyDays} day(s)",
        _ => "On-time return"
    };
}
