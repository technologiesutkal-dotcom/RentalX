using RentalX.Models;

namespace RentalX.Services;

public interface IMarketingPdfService
{
    string Generate(CompanySettings company);
}
