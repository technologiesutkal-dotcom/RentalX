using RentalX.Models;

namespace RentalX.Data.Repositories;

public interface ISettingsRepository
{
    Task<CompanySettings> GetAsync();
    Task SaveAsync(CompanySettings settings);

    /// <summary>
    /// Fills blank/default business settings from the client's Apply / portal profile.
    /// Does not overwrite values the shop has already customized.
    /// </summary>
    Task SyncFromClientProfileAsync(
        string? companyName,
        string? ownerName,
        string? phone,
        string? email,
        string? address,
        string? city);
}
