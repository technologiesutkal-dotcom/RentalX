using RentalX.Models.Licensing;

namespace RentalX.Services.Licensing;

public interface ILicenseClientService
{
    string ApiBaseUrl { get; set; }
    LicenseLocalState? Current { get; }
    bool HasLocalLicense { get; }
    Task<(bool Ok, string Message)> ActivateAsync(string licenseKey, CancellationToken ct = default);
    Task<(bool Ok, string Message)> ValidateAsync(CancellationToken ct = default);
    void ClearLocal();
}
