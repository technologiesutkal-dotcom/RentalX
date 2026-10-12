namespace RentalX.Models;

public class CompanySettings
{
    public int Id { get; set; } = 1;

    /// <summary>e.g. Event / Tent, Equipment, Vehicle, Costume, Camera, General rental</summary>
    public string BusinessType { get; set; } = "General rental";

    public string CompanyName { get; set; } = "My Rental Business";
    public string OwnerName { get; set; } = "Owner";
    public string Address { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string GstNumber { get; set; } = string.Empty;
    /// <summary>When false, GST is omitted from invoices and reports.</summary>
    public bool ShowGstOnReports { get; set; } = true;
    public string Currency { get; set; } = "INR (₹)";
    public string ReportFooter { get; set; } = "Thank you for your business";
    public bool EnableNotifications { get; set; } = true;
    public bool AutoBackup { get; set; } = true;
}
