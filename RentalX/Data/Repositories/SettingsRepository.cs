using Microsoft.EntityFrameworkCore;
using RentalX.Models;

namespace RentalX.Data.Repositories;

public class SettingsRepository : ISettingsRepository
{
    private readonly AppDbContext _db;

    public SettingsRepository(AppDbContext db)
    {
        _db = db;
    }

    public async Task<CompanySettings> GetAsync()
    {
        var settings = await _db.CompanySettings.AsNoTracking().FirstOrDefaultAsync(x => x.Id == 1);
        if (settings is not null)
        {
            return settings;
        }

        settings = new CompanySettings();
        _db.CompanySettings.Add(settings);
        await _db.SaveChangesAsync();
        return settings;
    }

    public async Task SaveAsync(CompanySettings settings)
    {
        settings.Id = 1;
        var existing = await _db.CompanySettings.FirstOrDefaultAsync(x => x.Id == 1);
        if (existing is null)
        {
            _db.CompanySettings.Add(settings);
        }
        else
        {
            _db.Entry(existing).CurrentValues.SetValues(settings);
        }

        await _db.SaveChangesAsync();
    }

    public async Task SyncFromClientProfileAsync(
        string? companyName,
        string? ownerName,
        string? phone,
        string? email,
        string? address,
        string? city)
    {
        var settings = await _db.CompanySettings.FirstOrDefaultAsync(x => x.Id == 1);
        if (settings is null)
        {
            settings = new CompanySettings();
            _db.CompanySettings.Add(settings);
        }

        var changed = false;

        if (IsBlankOrDefault(settings.CompanyName, "My Rental Business") && !string.IsNullOrWhiteSpace(companyName))
        {
            settings.CompanyName = companyName.Trim();
            changed = true;
        }

        if (IsBlankOrDefault(settings.OwnerName, "Owner") && !string.IsNullOrWhiteSpace(ownerName))
        {
            settings.OwnerName = ownerName.Trim();
            changed = true;
        }
        else if (IsBlankOrDefault(settings.OwnerName, "Owner") && !string.IsNullOrWhiteSpace(companyName))
        {
            settings.OwnerName = companyName.Trim();
            changed = true;
        }

        if (string.IsNullOrWhiteSpace(settings.Phone) && !string.IsNullOrWhiteSpace(phone))
        {
            settings.Phone = phone.Trim();
            changed = true;
        }

        if (string.IsNullOrWhiteSpace(settings.Email) && !string.IsNullOrWhiteSpace(email))
        {
            settings.Email = email.Trim();
            changed = true;
        }

        if (string.IsNullOrWhiteSpace(settings.Address) && !string.IsNullOrWhiteSpace(address))
        {
            settings.Address = address.Trim();
            changed = true;
        }

        if (string.IsNullOrWhiteSpace(settings.City) && !string.IsNullOrWhiteSpace(city))
        {
            settings.City = city.Trim();
            changed = true;
        }

        if (changed)
        {
            await _db.SaveChangesAsync();
        }
    }

    private static bool IsBlankOrDefault(string? value, string defaultValue) =>
        string.IsNullOrWhiteSpace(value) ||
        string.Equals(value.Trim(), defaultValue, StringComparison.OrdinalIgnoreCase);
}
