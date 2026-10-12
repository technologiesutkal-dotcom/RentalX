using RentalX.Models;

namespace RentalX.Services;

public interface IPdfReportService
{
    string GenerateBusinessReport(
        CompanySettings company,
        DateTime fromDate,
        DateTime toDate,
        ReportSummary summary,
        IReadOnlyList<ReportBookingRow> bookings,
        IReadOnlyList<Expense> expenses);
}
