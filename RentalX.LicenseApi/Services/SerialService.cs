using Microsoft.EntityFrameworkCore;
using RentalX.LicenseApi.Data;
using RentalX.LicenseApi.Dtos;
using RentalX.LicenseApi.Helpers;
using RentalX.LicenseApi.Models;

namespace RentalX.LicenseApi.Services;

public interface ISerialService
{
    Task<(SerialDto? Dto, string? Error)> ApplyAsync(SerialApplyRequest request, CancellationToken ct = default);
    Task<IReadOnlyList<SerialDto>> GetAllAsync(string? search, string? status, CancellationToken ct = default);
    Task<(SerialDto? Dto, string? Error)> ApproveAsync(int id, SerialApproveRequest request, CancellationToken ct = default);
    Task<(QuickApproveResponse? Response, string? Error)> QuickApproveAsync(int id, string? reviewNotes, string? planType = null, CancellationToken ct = default);
    Task<(SerialDto? Dto, string? Error)> RejectAsync(int id, SerialRejectRequest request, CancellationToken ct = default);
    Task<(SerialDto? Dto, string? Error)> RevokeAsync(int id, SerialRejectRequest request, CancellationToken ct = default);
    Task<(SerialDto? Dto, string? Error)> ConfigureAsync(SerialConfigureRequest request, CancellationToken ct = default);
    Task<(SerialDto? Dto, string? Message)> GetStatusAsync(string serialNumber, CancellationToken ct = default);
}

public class SerialService(LicenseDbContext db, IAdminCatalogService catalog) : ISerialService
{
    public async Task<(SerialDto? Dto, string? Error)> ApplyAsync(SerialApplyRequest request, CancellationToken ct = default)
    {
        var serial = SerialFormatter.Normalize(request.SerialNumber);
        if (serial.Length < 8)
        {
            return (null, "Serial number looks invalid.");
        }

        var existing = await db.SerialRegistrations
            .Include(s => s.Client)
            .Include(s => s.License)
            .FirstOrDefaultAsync(s => s.SerialNumber == serial, ct);

        // One laptop = one application only (any status blocks a second Apply).
        if (existing is not null)
        {
            var alreadyMsg = existing.Status switch
            {
                SerialStatuses.Approved => VendorMessages.ApplyAlreadyApproved,
                SerialStatuses.Pending => VendorMessages.ApplyAlreadyPending,
                SerialStatuses.Rejected => VendorMessages.ApplyAlreadyRejected,
                SerialStatuses.Deactivated => VendorMessages.ApplyAlreadyDeactivated,
                SerialStatuses.Revoked => VendorMessages.ApplyAlreadyRevoked,
                _ => VendorMessages.ApplyAlreadyExists
            };
            return (null, alreadyMsg);
        }

        var entity = new SerialRegistration
        {
            SerialNumber = serial,
            MachineName = string.IsNullOrWhiteSpace(request.MachineName) ? null : request.MachineName.Trim(),
            CompanyName = request.CompanyName.Trim(),
            ContactName = request.CompanyName.Trim(),
            Email = request.Email.Trim().ToLowerInvariant(),
            Phone = request.Phone.Trim(),
            Address = request.Address.Trim(),
            Pincode = request.Pincode.Trim(),
            Notes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim(),
            Status = SerialStatuses.Pending,
            AppliedAt = DateTime.UtcNow
        };
        db.SerialRegistrations.Add(entity);
        await db.SaveChangesAsync(ct);
        return (Map(entity), VendorMessages.ApplySubmitted);
    }

    public async Task<IReadOnlyList<SerialDto>> GetAllAsync(string? search, string? status, CancellationToken ct = default)
    {
        var q = db.SerialRegistrations.AsNoTracking()
            .Include(s => s.Client)
            .Include(s => s.License)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(status))
        {
            q = q.Where(s => s.Status == status);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToLowerInvariant();
            q = q.Where(x =>
                x.SerialNumber.ToLower().Contains(s) ||
                x.CompanyName.ToLower().Contains(s) ||
                x.ContactName.ToLower().Contains(s) ||
                x.Email.ToLower().Contains(s) ||
                (x.MachineName != null && x.MachineName.ToLower().Contains(s)));
        }

        var list = await q.OrderByDescending(x => x.AppliedAt).ToListAsync(ct);
        return list.Select(Map).ToList();
    }

    public async Task<(SerialDto? Dto, string? Error)> ApproveAsync(int id, SerialApproveRequest request, CancellationToken ct = default)
    {
        var entity = await db.SerialRegistrations
            .Include(s => s.Client)
            .Include(s => s.License)
            .FirstOrDefaultAsync(s => s.Id == id, ct);
        if (entity is null)
        {
            return (null, "Serial application not found.");
        }

        var client = await db.Clients.FirstOrDefaultAsync(c => c.Id == request.ClientId, ct);
        if (client is null)
        {
            return (null, "Client not found.");
        }

        if (client.Status != ClientStatuses.Active)
        {
            return (null, "Client must be Active before approving a serial.");
        }

        License? license = null;
        if (request.LicenseId is int lid)
        {
            license = await db.Licenses.FirstOrDefaultAsync(l => l.Id == lid && l.ClientId == client.Id, ct);
            if (license is null)
            {
                return (null, "License not found for this client.");
            }
        }
        else
        {
            license = await db.Licenses
                .Where(l => l.ClientId == client.Id && l.Status == LicenseStatuses.Active)
                .OrderByDescending(l => l.ExpiresAt)
                .FirstOrDefaultAsync(ct);
        }

        if (license is null)
        {
            return (null, "Client has no active license. Issue a license first, then approve this serial.");
        }

        entity.ClientId = client.Id;
        entity.LicenseId = license.Id;
        entity.Status = SerialStatuses.Approved;
        entity.ReviewedAt = DateTime.UtcNow;
        entity.ReviewNotes = string.IsNullOrWhiteSpace(request.ReviewNotes) ? "Approved by Super Admin" : request.ReviewNotes.Trim();
        entity.Client = client;
        entity.License = license;
        await db.SaveChangesAsync(ct);
        return (Map(entity), null);
    }

    /// <summary>
    /// One-click: create (or reuse) client + one license for chosen plan + approve this PC.
    /// </summary>
    public async Task<(QuickApproveResponse? Response, string? Error)> QuickApproveAsync(
        int id, string? reviewNotes, string? planType = null, CancellationToken ct = default)
    {
        var entity = await db.SerialRegistrations
            .Include(s => s.Client)
            .Include(s => s.License)
            .FirstOrDefaultAsync(s => s.Id == id, ct);
        if (entity is null)
        {
            return (null, "Request not found.");
        }

        if (entity.Status == SerialStatuses.Approved)
        {
            return (null, "Already approved. Tell the client to login with their credentials.");
        }

        if (entity.Status == SerialStatuses.Rejected)
        {
            return (null, "This request was Rejected. Ask the client to Apply again first.");
        }

        // Pending, Deactivated, or Revoked → approve / re-activate allowed
        var plan = NormalizePlan(planType);
        var email = entity.Email.Trim().ToLowerInvariant();
        var city = ExtractCity(entity.Address, entity.Pincode);
        var wasReactivate = entity.Status is SerialStatuses.Deactivated or SerialStatuses.Revoked;

        // Reuse existing client by email — never create a second profile for same shop.
        var client = await db.Clients.FirstOrDefaultAsync(c => c.Email.ToLower() == email, ct);
        if (client is null && entity.ClientId is int linkedId)
        {
            client = await db.Clients.FirstOrDefaultAsync(c => c.Id == linkedId, ct);
        }

        if (client is null)
        {
            client = new Client
            {
                CompanyName = entity.CompanyName.Trim(),
                ContactName = string.IsNullOrWhiteSpace(entity.ContactName) ? entity.CompanyName.Trim() : entity.ContactName.Trim(),
                Email = email,
                Phone = entity.Phone.Trim(),
                City = city,
                Address = string.IsNullOrWhiteSpace(entity.Address)
                    ? null
                    : $"{entity.Address.Trim()}{(string.IsNullOrWhiteSpace(entity.Pincode) ? "" : ", " + entity.Pincode.Trim())}",
                Notes = "Created from Apply request",
                Status = ClientStatuses.Active,
                CreatedAt = DateTime.UtcNow
            };
            db.Clients.Add(client);
            await db.SaveChangesAsync(ct);
        }
        else
        {
            client.CompanyName = entity.CompanyName.Trim();
            client.ContactName = string.IsNullOrWhiteSpace(entity.ContactName)
                ? entity.CompanyName.Trim()
                : entity.ContactName.Trim();
            client.Phone = entity.Phone.Trim();
            client.City = city ?? client.City;
            client.Email = email;
            client.Address = string.IsNullOrWhiteSpace(entity.Address)
                ? client.Address
                : $"{entity.Address.Trim()}{(string.IsNullOrWhiteSpace(entity.Pincode) ? "" : ", " + entity.Pincode.Trim())}";
            client.Status = ClientStatuses.Active;
            client.UpdatedAt = DateTime.UtcNow;
            await db.SaveChangesAsync(ct);
        }

        // Always reset password on approve / re-approve so Super Admin can share known credentials.
        var tempPassword = PortalCredentials.DefaultPassword;
        var loginId = await EnsurePortalUserWithPasswordAsync(client, tempPassword, ct);

        var starts = DateTimeUtc.UtcToday;
        var expires = DateTimeUtc.AsUtcDate(plan switch
        {
            PlanTypes.Trial => starts.AddDays(14),
            PlanTypes.Monthly => starts.AddMonths(1),
            PlanTypes.Lifetime => starts.AddYears(100),
            _ => starts.AddYears(1)
        });

        // One license only — reactivates revoked license on re-approve.
        var license = await catalog.UpsertActiveLicenseAsync(
            client.Id, plan, 1, starts, expires,
            wasReactivate ? "Re-activated on approve" : "Issued on approve", ct);

        entity.ClientId = client.Id;
        entity.LicenseId = license.Id;
        entity.Status = SerialStatuses.Approved;
        entity.ReviewedAt = DateTime.UtcNow;
        entity.ReviewNotes = string.IsNullOrWhiteSpace(reviewNotes)
            ? (wasReactivate ? $"Re-activated — {plan} plan" : $"Approved — {plan} plan")
            : reviewNotes.Trim();
        entity.Client = client;
        entity.License = license;
        await db.SaveChangesAsync(ct);

        var dto = Map(entity);
        var message =
            $"{(wasReactivate ? "Re-activated" : "Approved")} {dto.DisplaySerial}.\n\n" +
            $"Company: {client.CompanyName}\n" +
            $"Plan: {plan}\n\n" +
            $"CLIENT LOGIN (use these now):\n" +
            $"Login ID: {loginId}\n" +
            $"Password: {tempPassword}\n" +
            $"License: {license.LicenseKey}\n\n" +
            "Client can sign in on that computer now.";

        return (new QuickApproveResponse(dto, loginId, tempPassword, license.LicenseKey, message), null);
    }

    private static string NormalizePlan(string? planType)
    {
        var p = (planType ?? PlanTypes.Yearly).Trim();
        return p switch
        {
            PlanTypes.Trial or PlanTypes.Monthly or PlanTypes.Yearly or PlanTypes.Lifetime => p,
            _ => PlanTypes.Yearly
        };
    }

    /// <summary>Create or reactivate portal user and always set the given password.</summary>
    private async Task<string> EnsurePortalUserWithPasswordAsync(Client client, string password, CancellationToken ct)
    {
        var (hash, salt) = PasswordHasher.HashPassword(password);
        var existing = await db.ClientUsers.FirstOrDefaultAsync(u => u.ClientId == client.Id, ct);
        if (existing is not null)
        {
            existing.DisplayName = client.CompanyName;
            existing.IsActive = true;
            existing.PasswordHash = hash;
            existing.PasswordSalt = salt;
            existing.PortalPassword = password;
            await db.SaveChangesAsync(ct);
            return existing.Username;
        }

        var username = await PortalCredentials.AllocateNextLoginIdAsync(db, ct);
        db.ClientUsers.Add(new ClientUser
        {
            ClientId = client.Id,
            Username = username,
            PasswordHash = hash,
            PasswordSalt = salt,
            PortalPassword = password,
            DisplayName = client.CompanyName,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync(ct);
        return username;
    }

    private static string? ExtractCity(string? address, string? pincode)
    {
        if (string.IsNullOrWhiteSpace(address))
        {
            return string.IsNullOrWhiteSpace(pincode) ? null : pincode.Trim();
        }

        var parts = address.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        return parts.Length > 0 ? parts[^1] : address.Trim();
    }

    public async Task<(SerialDto? Dto, string? Error)> RejectAsync(int id, SerialRejectRequest request, CancellationToken ct = default)
    {
        var entity = await Load(id, ct);
        if (entity is null)
        {
            return (null, "Serial application not found.");
        }

        entity.Status = SerialStatuses.Rejected;
        entity.ReviewedAt = DateTime.UtcNow;
        entity.ReviewNotes = string.IsNullOrWhiteSpace(request.ReviewNotes) ? "Rejected by Super Admin" : request.ReviewNotes.Trim();
        entity.ClientId = null;
        entity.LicenseId = null;
        await db.SaveChangesAsync(ct);
        return (Map(entity), null);
    }

    public async Task<(SerialDto? Dto, string? Error)> RevokeAsync(int id, SerialRejectRequest request, CancellationToken ct = default)
    {
        var entity = await Load(id, ct);
        if (entity is null)
        {
            return (null, "Serial registration not found.");
        }

        // After approve, Deactivate shows clearly and blocks login.
        entity.Status = SerialStatuses.Deactivated;
        entity.ReviewedAt = DateTime.UtcNow;
        entity.ReviewNotes = string.IsNullOrWhiteSpace(request.ReviewNotes)
            ? "Deactivated by Super Admin"
            : request.ReviewNotes.Trim();

        if (entity.ClientId is int clientId)
        {
            var client = await db.Clients.FirstOrDefaultAsync(c => c.Id == clientId, ct);
            if (client is not null)
            {
                client.Status = ClientStatuses.Deactivated;
                client.UpdatedAt = DateTime.UtcNow;
            }

            foreach (var license in await db.Licenses.Where(l => l.ClientId == clientId && l.Status == LicenseStatuses.Active).ToListAsync(ct))
            {
                license.Status = LicenseStatuses.Revoked;
                license.Notes = string.IsNullOrWhiteSpace(license.Notes)
                    ? "Deactivated with client"
                    : license.Notes + " | Deactivated";
            }

            var licenseIds = await db.Licenses.Where(l => l.ClientId == clientId).Select(l => l.Id).ToListAsync(ct);
            foreach (var activation in await db.Activations
                         .Where(a => licenseIds.Contains(a.LicenseId) && a.IsActive)
                         .ToListAsync(ct))
            {
                activation.IsActive = false;
                activation.DeactivatedAt = DateTime.UtcNow;
                activation.DeactivateReason = "Client deactivated";
            }

            await ClientAccessGuard.DisableClientUsersAsync(db, clientId, ct);
            await ClientAccessGuard.RevokeSessionsForClientAsync(db, clientId, ct);
        }

        await db.SaveChangesAsync(ct);
        return (Map(entity), null);
    }

    public async Task<(SerialDto? Dto, string? Error)> ConfigureAsync(SerialConfigureRequest request, CancellationToken ct = default)
    {
        var serial = SerialFormatter.Normalize(request.SerialNumber);
        if (serial.Length < 8)
        {
            return (null, "Serial number looks invalid.");
        }

        var client = await db.Clients.FirstOrDefaultAsync(c => c.Id == request.ClientId, ct);
        if (client is null)
        {
            return (null, "Client not found.");
        }

        License? license = null;
        if (request.LicenseId is int lid)
        {
            license = await db.Licenses.FirstOrDefaultAsync(l => l.Id == lid && l.ClientId == client.Id, ct);
            if (license is null)
            {
                return (null, "License not found for this client.");
            }
        }
        else
        {
            license = await db.Licenses
                .Where(l => l.ClientId == client.Id && l.Status == LicenseStatuses.Active)
                .OrderByDescending(l => l.ExpiresAt)
                .FirstOrDefaultAsync(ct);
        }

        if (license is null)
        {
            return (null, "Issue an active license for this client before configuring the serial.");
        }

        var entity = await db.SerialRegistrations
            .Include(s => s.Client)
            .Include(s => s.License)
            .FirstOrDefaultAsync(s => s.SerialNumber == serial, ct);

        if (entity is null)
        {
            entity = new SerialRegistration
            {
                SerialNumber = serial,
                MachineName = string.IsNullOrWhiteSpace(request.MachineName) ? null : request.MachineName.Trim(),
                CompanyName = client.CompanyName,
                ContactName = client.ContactName,
                Email = client.Email,
                Phone = client.Phone,
                AppliedAt = DateTime.UtcNow
            };
            db.SerialRegistrations.Add(entity);
        }

        entity.ClientId = client.Id;
        entity.LicenseId = license.Id;
        entity.Status = SerialStatuses.Approved;
        entity.ReviewedAt = DateTime.UtcNow;
        entity.ReviewNotes = string.IsNullOrWhiteSpace(request.ReviewNotes)
            ? "Configured directly by Super Admin"
            : request.ReviewNotes.Trim();
        if (!string.IsNullOrWhiteSpace(request.MachineName))
        {
            entity.MachineName = request.MachineName.Trim();
        }

        await db.SaveChangesAsync(ct);
        entity.Client = client;
        entity.License = license;
        return (Map(entity), null);
    }

    public async Task<(SerialDto? Dto, string? Message)> GetStatusAsync(string serialNumber, CancellationToken ct = default)
    {
        var serial = SerialFormatter.Normalize(serialNumber);
        var entity = await db.SerialRegistrations.AsNoTracking()
            .Include(s => s.Client)
            .Include(s => s.License)
            .FirstOrDefaultAsync(s => s.SerialNumber == serial, ct);

        if (entity is null)
        {
            return (null, "Serial not registered. Please Apply for Access.");
        }

        var msg = entity.Status switch
        {
            SerialStatuses.Pending => VendorMessages.PendingApproval,
            SerialStatuses.Approved => "Activated. You can sign in on this computer.",
            SerialStatuses.Rejected => VendorMessages.Rejected,
            SerialStatuses.Revoked => VendorMessages.Revoked,
            SerialStatuses.Deactivated => VendorMessages.Deactivated,
            _ => entity.Status
        };
        return (Map(entity), msg);
    }

    private async Task<SerialRegistration?> Load(int id, CancellationToken ct) =>
        await db.SerialRegistrations
            .Include(s => s.Client)
            .Include(s => s.License)
            .FirstOrDefaultAsync(s => s.Id == id, ct);

    private static SerialDto Map(SerialRegistration s) =>
        new(s.Id, s.SerialNumber, SerialFormatter.Format(s.SerialNumber), s.MachineName,
            s.CompanyName, s.ContactName, s.Email, s.Phone, s.Address, s.Pincode, s.Notes, s.Status,
            s.ClientId, s.Client?.CompanyName, s.LicenseId, s.License?.LicenseKey,
            s.AppliedAt, s.ReviewedAt, s.ReviewNotes);
}
