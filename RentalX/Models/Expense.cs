namespace RentalX.Models;

public class Expense
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Category { get; set; } = "General";
    public decimal Amount { get; set; }
    public DateTime ExpenseDate { get; set; } = DateTime.Today;
    public string Notes { get; set; } = string.Empty;

    public string AmountText => $"₹{Amount:N0}";
    public string ExpenseDateText => ExpenseDate.ToString("dd MMM yyyy");
}
