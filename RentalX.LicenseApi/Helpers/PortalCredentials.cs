using Microsoft.EntityFrameworkCore;
using RentalX.LicenseApi.Data;

namespace RentalX.LicenseApi.Helpers;

/// <summary>
/// Shared client portal Login ID / default password rules.
/// Login IDs: 55041, 55042, …  Default password for all new / reset clients.
/// </summary>
public static class PortalCredentials
{
    public const int StartingLoginId = 55041;

    /// <summary>Default password given to every new client (easy to share, strong enough to start).</summary>
    public const string DefaultPassword = "RxShop@2026";

    public static async Task<string> AllocateNextLoginIdAsync(LicenseDbContext db, CancellationToken ct = default)
    {
        var numericIds = await db.ClientUsers
            .AsNoTracking()
            .Select(u => u.Username)
            .ToListAsync(ct);

        var max = StartingLoginId - 1;
        foreach (var name in numericIds)
        {
            if (int.TryParse(name, out var n) && n >= StartingLoginId && n > max)
            {
                max = n;
            }
        }

        var next = max + 1;
        // Safety: skip if somehow already taken as non-numeric collision path
        while (await db.ClientUsers.AnyAsync(u => u.Username == next.ToString(), ct))
        {
            next++;
        }

        return next.ToString();
    }
}
