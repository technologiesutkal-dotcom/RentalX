using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using RentalX.LicenseApi.Data;
using RentalX.LicenseApi.Dtos;
using RentalX.LicenseApi.Helpers;
using RentalX.LicenseApi.Models;
// VendorMessages in Helpers

namespace RentalX.LicenseApi.Services;

public interface IAuthService
{
    Task<(LoginResponse? Response, string? Error)> LoginAsync(LoginRequest request, CancellationToken ct = default);
    Task<AdminUser?> ValidateTokenAsync(string? token, CancellationToken ct = default);
    /// <summary>Returns whether a client bearer token is still allowed to use the app.</summary>
    Task<(bool Allowed, string Message)> CheckClientAccessAsync(string? token, CancellationToken ct = default);
    Task LogoutAsync(string? token, CancellationToken ct = default);
    Task<(string? Username, string? Error)> ChangeAdminCredentialsAsync(
        string? token,
        string currentPassword,
        string newUsername,
        string newPassword,
        CancellationToken ct = default);
}

public class AuthService(LicenseDbContext db) : IAuthService
{
    private static readonly TimeSpan TokenLifetime = TimeSpan.FromHours(12);

    public async Task<(LoginResponse? Response, string? Error)> LoginAsync(LoginRequest request, CancellationToken ct = default)
    {
        var username = request.Username.Trim().ToLowerInvariant();

        var admin = await db.AdminUsers.FirstOrDefaultAsync(
            x => x.Username.ToLower() == username && x.IsActive, ct);

        if (admin is not null)
        {
            if (!PasswordHasher.Verify(request.Password, admin.PasswordHash, admin.PasswordSalt))
            {
                return (null, "Invalid login ID or password.");
            }

            var adminSerial = SerialFormatter.Normalize(request.SerialNumber);
            var hasLockedPcs = await db.AdminSerials.AnyAsync(s => s.IsActive, ct);

            if (!hasLockedPcs)
            {
                // First-time bootstrap: any PC can sign in. Auto-register this laptop
                // so from now on only configured Super Admin PCs are allowed.
                if (string.IsNullOrWhiteSpace(adminSerial))
                {
                    return (null, "Computer serial is required for Super Admin login.");
                }

                db.AdminSerials.Add(new AdminSerial
                {
                    SerialNumber = adminSerial,
                    MachineName = null,
                    Notes = "Auto-registered on first Super Admin login",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    LastLoginAt = DateTime.UtcNow
                });
            }
            else
            {
                if (string.IsNullOrWhiteSpace(adminSerial))
                {
                    return (null, "Computer serial is required for Super Admin login.");
                }

                var allowed = await db.AdminSerials.FirstOrDefaultAsync(
                    s => s.IsActive && s.SerialNumber == adminSerial, ct);

                // Accept older rows that stored truncated display serials / RX prefix.
                allowed ??= await db.AdminSerials.FirstOrDefaultAsync(s =>
                    s.IsActive &&
                    (s.SerialNumber == "RX" + adminSerial ||
                     adminSerial.StartsWith(s.SerialNumber) ||
                     s.SerialNumber.StartsWith(adminSerial)), ct);

                if (allowed is null)
                {
                    return (null,
                        "This laptop is not authorized for Super Admin. " +
                        "Ask an existing Super Admin to add this serial under Account → Super Admin laptops.");
                }

                // Repair stored value to the full machine id used at login.
                if (allowed.SerialNumber != adminSerial)
                {
                    allowed.SerialNumber = adminSerial;
                }

                allowed.LastLoginAt = DateTime.UtcNow;
            }

            var token = NewToken();
            var expires = DateTime.UtcNow.Add(TokenLifetime);
            db.AdminSessions.Add(new AdminSession
            {
                AdminUserId = admin.Id,
                Token = token,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = expires
            });
            await db.SaveChangesAsync(ct);
            return (new LoginResponse(
                token,
                admin.DisplayName,
                expires,
                UserRoles.SuperAdmin,
                Username: admin.Username,
                SerialNumber: SerialFormatter.Format(adminSerial)), null);
        }

        var clientUser = await db.ClientUsers
            .Include(x => x.Client)
            .FirstOrDefaultAsync(x => x.Username.ToLower() == username, ct);

        if (clientUser is null ||
            !PasswordHasher.Verify(request.Password, clientUser.PasswordHash, clientUser.PasswordSalt))
        {
            return (null, "Invalid login ID or password.");
        }

        if (!clientUser.IsActive)
        {
            return (null, VendorMessages.Deactivated);
        }

        if (clientUser.Client is null)
        {
            return (null, VendorMessages.ContactShort);
        }

        if (clientUser.Client.Status == ClientStatuses.Suspended)
        {
            return (null, VendorMessages.Suspended);
        }

        if (clientUser.Client.Status is ClientStatuses.Inactive or ClientStatuses.Deactivated)
        {
            return (null, clientUser.Client.Status == ClientStatuses.Deactivated
                ? VendorMessages.Deactivated
                : VendorMessages.Inactive);
        }

        var hasActiveLicense = await db.Licenses.AnyAsync(l =>
            l.ClientId == clientUser.ClientId &&
            l.Status == LicenseStatuses.Active &&
            (l.PlanType == PlanTypes.Lifetime || l.ExpiresAt.Date >= DateTime.UtcNow.Date), ct);

        if (!hasActiveLicense)
        {
            return (null, VendorMessages.NoLicense);
        }

        var serial = SerialFormatter.Normalize(request.SerialNumber);
        if (string.IsNullOrWhiteSpace(serial))
        {
            return (null, "Computer serial number is required for login.");
        }

        var registration = await db.SerialRegistrations
            .FirstOrDefaultAsync(s => s.SerialNumber == serial, ct);

        if (registration is null)
        {
            return (null, VendorMessages.NotRegistered);
        }

        if (registration.Status == SerialStatuses.Pending)
        {
            return (null, VendorMessages.PendingApproval);
        }

        if (registration.Status == SerialStatuses.Rejected)
        {
            return (null, VendorMessages.Rejected);
        }

        if (registration.Status is SerialStatuses.Revoked or SerialStatuses.Deactivated)
        {
            return (null, registration.Status == SerialStatuses.Deactivated
                ? VendorMessages.Deactivated
                : VendorMessages.Revoked);
        }

        if (registration.Status != SerialStatuses.Approved)
        {
            return (null, VendorMessages.ContactShort);
        }

        if (registration.ClientId != clientUser.ClientId)
        {
            return (null, VendorMessages.SerialMismatch);
        }

        var clientToken = NewToken();
        var clientExpires = DateTime.UtcNow.Add(TokenLifetime);
        db.ClientSessions.Add(new ClientSession
        {
            ClientUserId = clientUser.Id,
            Token = clientToken,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = clientExpires
        });
        await db.SaveChangesAsync(ct);

        var client = clientUser.Client;
        return (new LoginResponse(
            clientToken,
            clientUser.DisplayName,
            clientExpires,
            UserRoles.Client,
            clientUser.ClientId,
            client.CompanyName,
            SerialFormatter.Format(serial),
            clientUser.Username,
            string.IsNullOrWhiteSpace(client.ContactName) ? client.CompanyName : client.ContactName,
            client.Email,
            client.Phone,
            client.Address,
            client.City), null);
    }

    public async Task<AdminUser?> ValidateTokenAsync(string? token, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return null;
        }

        var now = DateTime.UtcNow;
        var session = await db.AdminSessions
            .Include(x => x.AdminUser)
            .FirstOrDefaultAsync(x =>
                x.Token == token &&
                x.RevokedAt == null &&
                x.ExpiresAt > now, ct);

        if (session?.AdminUser is null || !session.AdminUser.IsActive)
        {
            return null;
        }

        return session.AdminUser;
    }

    public async Task<(bool Allowed, string Message)> CheckClientAccessAsync(string? token, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return (false, "Not signed in.");
        }

        var now = DateTime.UtcNow;
        var session = await db.ClientSessions
            .Include(s => s.ClientUser)!.ThenInclude(u => u!.Client)
            .FirstOrDefaultAsync(s => s.Token == token, ct);

        if (session is null || session.RevokedAt is not null || session.ExpiresAt <= now)
        {
            return (false, VendorMessages.Deactivated);
        }

        var user = session.ClientUser;
        if (user is null || !user.IsActive)
        {
            return (false, VendorMessages.Deactivated);
        }

        var client = user.Client;
        if (client is null || client.Status is ClientStatuses.Inactive or ClientStatuses.Deactivated or ClientStatuses.Suspended)
        {
            return (false, client?.Status == ClientStatuses.Suspended
                ? VendorMessages.Suspended
                : VendorMessages.Deactivated);
        }

        return (true, "Access OK");
    }

    public async Task LogoutAsync(string? token, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return;
        }

        var adminSession = await db.AdminSessions.FirstOrDefaultAsync(x => x.Token == token, ct);
        if (adminSession is not null && adminSession.RevokedAt is null)
        {
            adminSession.RevokedAt = DateTime.UtcNow;
            await db.SaveChangesAsync(ct);
            return;
        }

        var clientSession = await db.ClientSessions.FirstOrDefaultAsync(x => x.Token == token, ct);
        if (clientSession is not null && clientSession.RevokedAt is null)
        {
            clientSession.RevokedAt = DateTime.UtcNow;
            await db.SaveChangesAsync(ct);
        }
    }

    public async Task<(string? Username, string? Error)> ChangeAdminCredentialsAsync(
        string? token,
        string currentPassword,
        string newUsername,
        string newPassword,
        CancellationToken ct = default)
    {
        var admin = await ValidateTokenAsync(token, ct);
        if (admin is null)
        {
            return (null, "Unauthorized.");
        }

        if (!PasswordHasher.Verify(currentPassword, admin.PasswordHash, admin.PasswordSalt))
        {
            return (null, "Current password is incorrect.");
        }

        var username = newUsername.Trim().ToLowerInvariant();
        if (username.Length < 3)
        {
            return (null, "New username must be at least 3 characters.");
        }

        if (newPassword.Trim().Length < 6)
        {
            return (null, "New password must be at least 6 characters.");
        }

        if (await db.AdminUsers.AnyAsync(a => a.Id != admin.Id && a.Username.ToLower() == username, ct))
        {
            return (null, "That username is already taken.");
        }

        if (await db.ClientUsers.AnyAsync(u => u.Username.ToLower() == username, ct))
        {
            return (null, "That username is already used by a client login.");
        }

        var tracked = await db.AdminUsers.FirstAsync(a => a.Id == admin.Id, ct);
        var (hash, salt) = PasswordHasher.HashPassword(newPassword.Trim());
        tracked.Username = username;
        tracked.PasswordHash = hash;
        tracked.PasswordSalt = salt;
        tracked.DisplayName = string.IsNullOrWhiteSpace(tracked.DisplayName) ? "Super Admin" : tracked.DisplayName;
        await db.SaveChangesAsync(ct);
        return (tracked.Username, null);
    }

    private static string NewToken() => Convert.ToBase64String(RandomNumberGenerator.GetBytes(48));
}
