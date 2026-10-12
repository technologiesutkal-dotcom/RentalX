using Microsoft.EntityFrameworkCore;
using RentalX.LicenseApi.Data;
using RentalX.LicenseApi.Dtos;
using RentalX.LicenseApi.Helpers;
using RentalX.LicenseApi.Models;

namespace RentalX.LicenseApi.Services;

public interface ILicenseOpsService
{
    Task<LicenseStatusResponse> ActivateAsync(ActivateRequest request, CancellationToken ct = default);
    Task<LicenseStatusResponse> ValidateAsync(LicenseCheckRequest request, CancellationToken ct = default);
}

public class LicenseOpsService(LicenseDbContext db) : ILicenseOpsService
{
    public async Task<LicenseStatusResponse> ActivateAsync(ActivateRequest request, CancellationToken ct = default)
    {
        var key = LicenseKeyGenerator.Normalize(request.LicenseKey);
        var machineId = request.MachineId.Trim();

        var license = await db.Licenses
            .Include(x => x.Client)
            .Include(x => x.Activations)
            .FirstOrDefaultAsync(x => x.LicenseKey == key, ct);

        if (license is null)
        {
            return Fail("License key not found.");
        }

        RefreshExpired(license);

        if (license.Client?.Status == ClientStatuses.Suspended)
        {
            return Fail("Client account is suspended.", license);
        }

        if (license.Status == LicenseStatuses.Revoked)
        {
            return Fail("License has been revoked.", license);
        }

        if (license.Status == LicenseStatuses.Suspended)
        {
            return Fail("License is suspended.", license);
        }

        if (license.Status == LicenseStatuses.Expired || license.ExpiresAt.Date < DateTime.UtcNow.Date)
        {
            license.Status = LicenseStatuses.Expired;
            await db.SaveChangesAsync(ct);
            return Fail("License has expired.", license);
        }

        var existing = license.Activations.FirstOrDefault(a =>
            a.MachineId.Equals(machineId, StringComparison.OrdinalIgnoreCase));

        if (existing is not null)
        {
            existing.IsActive = true;
            existing.DeactivatedAt = null;
            existing.DeactivateReason = null;
            existing.LastSeenAt = DateTime.UtcNow;
            existing.MachineName = request.MachineName ?? existing.MachineName;
            existing.OsInfo = request.OsInfo ?? existing.OsInfo;
            await db.SaveChangesAsync(ct);
            return Ok("License re-activated on this machine.", license);
        }

        var activeCount = license.Activations.Count(a => a.IsActive);
        if (activeCount >= license.MaxMachines)
        {
            return Fail($"Machine limit reached ({license.MaxMachines}). Deactivate another device first.", license);
        }

        db.Activations.Add(new Activation
        {
            LicenseId = license.Id,
            MachineId = machineId,
            MachineName = request.MachineName,
            OsInfo = request.OsInfo,
            ActivatedAt = DateTime.UtcNow,
            LastSeenAt = DateTime.UtcNow,
            IsActive = true
        });
        await db.SaveChangesAsync(ct);
        return Ok("License activated successfully.", license);
    }

    public async Task<LicenseStatusResponse> ValidateAsync(LicenseCheckRequest request, CancellationToken ct = default)
    {
        var key = LicenseKeyGenerator.Normalize(request.LicenseKey);
        var machineId = request.MachineId.Trim();

        var license = await db.Licenses
            .Include(x => x.Client)
            .Include(x => x.Activations)
            .FirstOrDefaultAsync(x => x.LicenseKey == key, ct);

        if (license is null)
        {
            return Fail("License key not found.");
        }

        RefreshExpired(license);

        if (license.Client?.Status == ClientStatuses.Suspended)
        {
            return Fail("Client account is suspended.", license);
        }

        if (license.Status != LicenseStatuses.Active)
        {
            return Fail($"License status: {license.Status}.", license);
        }

        if (license.ExpiresAt.Date < DateTime.UtcNow.Date)
        {
            license.Status = LicenseStatuses.Expired;
            await db.SaveChangesAsync(ct);
            return Fail("License has expired.", license);
        }

        var activation = license.Activations.FirstOrDefault(a =>
            a.IsActive && a.MachineId.Equals(machineId, StringComparison.OrdinalIgnoreCase));

        if (activation is null)
        {
            return Fail("This machine is not activated for this license.", license);
        }

        activation.LastSeenAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return Ok("License is valid.", license);
    }

    private static void RefreshExpired(License license)
    {
        if (license.Status == LicenseStatuses.Active &&
            license.PlanType != PlanTypes.Lifetime &&
            license.ExpiresAt.Date < DateTime.UtcNow.Date)
        {
            license.Status = LicenseStatuses.Expired;
        }
    }

    private static LicenseStatusResponse Ok(string message, License license) =>
        new(true, message, license.Client?.CompanyName, license.PlanType, license.ExpiresAt,
            DaysRemaining(license), license.MaxMachines, license.Status);

    private static LicenseStatusResponse Fail(string message, License? license = null) =>
        new(false, message, license?.Client?.CompanyName, license?.PlanType, license?.ExpiresAt,
            license is null ? null : DaysRemaining(license), license?.MaxMachines, license?.Status);

    private static int DaysRemaining(License license)
    {
        if (license.PlanType == PlanTypes.Lifetime)
        {
            return 36500;
        }

        return Math.Max(0, (license.ExpiresAt.Date - DateTime.UtcNow.Date).Days);
    }
}
