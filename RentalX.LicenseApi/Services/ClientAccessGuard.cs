using Microsoft.EntityFrameworkCore;
using RentalX.LicenseApi.Data;
using RentalX.LicenseApi.Models;

namespace RentalX.LicenseApi.Services;

/// <summary>Shared helpers when Super Admin deactivates a client / serial.</summary>
public static class ClientAccessGuard
{
    public static async Task RevokeSessionsForClientAsync(LicenseDbContext db, int clientId, CancellationToken ct = default)
    {
        var userIds = await db.ClientUsers
            .Where(u => u.ClientId == clientId)
            .Select(u => u.Id)
            .ToListAsync(ct);

        if (userIds.Count == 0)
        {
            return;
        }

        var sessions = await db.ClientSessions
            .Where(s => userIds.Contains(s.ClientUserId) && s.RevokedAt == null)
            .ToListAsync(ct);

        var now = DateTime.UtcNow;
        foreach (var session in sessions)
        {
            session.RevokedAt = now;
        }
    }

    public static async Task DisableClientUsersAsync(LicenseDbContext db, int clientId, CancellationToken ct = default)
    {
        foreach (var user in await db.ClientUsers.Where(u => u.ClientId == clientId && u.IsActive).ToListAsync(ct))
        {
            user.IsActive = false;
        }
    }
}
