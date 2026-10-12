using RentalX.Models;

namespace RentalX.Services;

public interface IInvoicePdfService
{
    string GenerateInvoice(CompanySettings company, Booking booking);
}
