using Microsoft.EntityFrameworkCore;
using RentalX.LicenseApi.Data;
using RentalX.LicenseApi.Dtos;
using RentalX.LicenseApi.Helpers;
using RentalX.LicenseApi.Models;

namespace RentalX.LicenseApi.Services;

public interface IAdminCatalogService
{
    Task<DashboardStatsDto> GetDashboardAsync(CancellationToken ct = default);
    Task<IReadOnlyList<ClientDto>> GetClientsAsync(string? search, CancellationToken ct = default);
    Task<ClientDto?> GetClientAsync(int id, CancellationToken ct = default);
    Task<(ClientDto? Dto, string? Error)> CreateClientAsync(ClientUpsertRequest request, CancellationToken ct = default);
    Task<(ClientDto? Dto, string? Error)> UpdateClientAsync(int id, ClientUpsertRequest request, CancellationToken ct = default);
    Task<IReadOnlyList<LicenseDto>> GetLicensesAsync(string? search, int? clientId, CancellationToken ct = default);
    Task<(LicenseDto? Dto, string? Error)> CreateLicenseAsync(LicenseCreateRequest request, CancellationToken ct = default);
    Task<License> UpsertActiveLicenseAsync(int clientId, string planType, int maxMachines, DateTime starts, DateTime expires, string? notes, CancellationToken ct = default);
    Task<(LicenseDto? Dto, string? Error)> UpdateLicenseAsync(int id, LicenseUpdateRequest request, CancellationToken ct = default);
    Task<(LicenseDto? Dto, string? Error)> ExtendLicenseAsync(int id, ExtendLicenseRequest request, CancellationToken ct = default);
    Task<(LicenseDto? Dto, string? Error)> RevokeLicenseAsync(int id, CancellationToken ct = default);
    Task<IReadOnlyList<ActivationDto>> GetActivationsAsync(string? search, bool? activeOnly, CancellationToken ct = default);
    Task<(ActivationDto? Dto, string? Error)> DeactivateAsync(int id, DeactivateRequest request, CancellationToken ct = default);
    Task<IReadOnlyList<PaymentDto>> GetPaymentsAsync(string? search, CancellationToken ct = default);
    Task<(PaymentDto? Dto, string? Error)> CreatePaymentAsync(PaymentUpsertRequest request, CancellationToken ct = default);
    Task<(PaymentDto? Dto, string? Error)> UpdatePaymentAsync(int id, PaymentUpsertRequest request, CancellationToken ct = default);
    Task<(ClientDto? Dto, string? Error)> DeactivateClientAsync(int id, CancellationToken ct = default);
    Task<(ClientDto? Dto, string? Error)> ReactivateClientAsync(int id, CancellationToken ct = default);
    Task<(string? Message, string? Error)> FactoryResetAsync(CancellationToken ct = default);
    Task<IReadOnlyList<AdminSerialDto>> GetAdminSerialsAsync(CancellationToken ct = default);
    Task<(AdminSerialDto? Dto, string? Error)> AddAdminSerialAsync(AdminSerialUpsertRequest request, CancellationToken ct = default);
    Task<(string? Message, string? Error)> RemoveAdminSerialAsync(int id, CancellationToken ct = default);
}

public class AdminCatalogService(LicenseDbContext db) : IAdminCatalogService
{
    private static readonly HashSet<string> ValidClientStatuses =
        [ClientStatuses.Active, ClientStatuses.Suspended, ClientStatuses.Inactive, ClientStatuses.Deactivated];

    private static readonly HashSet<string> ValidPlanTypes =
        [PlanTypes.Trial, PlanTypes.Monthly, PlanTypes.Yearly, PlanTypes.Lifetime];

    private static readonly HashSet<string> ValidLicenseStatuses =
        [LicenseStatuses.Active, LicenseStatuses.Expired, LicenseStatuses.Revoked, LicenseStatuses.Suspended];

    private static readonly HashSet<string> ValidPaymentMethods =
        [PaymentMethods.Manual, PaymentMethods.Upi, PaymentMethods.BankTransfer, PaymentMethods.Razorpay, PaymentMethods.Cash];

    private static readonly HashSet<string> ValidPaymentStatuses =
        [PaymentStatuses.Pending, PaymentStatuses.Completed, PaymentStatuses.Failed, PaymentStatuses.Refunded];

    public async Task<DashboardStatsDto> GetDashboardAsync(CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        var monthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var soon = now.Date.AddDays(30);

        var clients = await db.Clients.AsNoTracking().ToListAsync(ct);
        var licenses = await db.Licenses.AsNoTracking().ToListAsync(ct);
        var activations = await db.Activations.AsNoTracking().ToListAsync(ct);
        var payments = await db.Payments.AsNoTracking().ToListAsync(ct);
        var pendingSerials = await db.SerialRegistrations.AsNoTracking()
            .CountAsync(s => s.Status == SerialStatuses.Pending, ct);

        foreach (var license in licenses.Where(l =>
                     l.Status == LicenseStatuses.Active &&
                     l.PlanType != PlanTypes.Lifetime &&
                     l.ExpiresAt.Date < now.Date))
        {
            license.Status = LicenseStatuses.Expired;
        }

        return new DashboardStatsDto(
            clients.Count,
            clients.Count(c => c.Status == ClientStatuses.Active),
            licenses.Count(l => l.Status == LicenseStatuses.Active),
            licenses.Count(l =>
                l.Status == LicenseStatuses.Active &&
                l.PlanType != PlanTypes.Lifetime &&
                l.ExpiresAt.Date <= soon),
            activations.Count(a => a.IsActive),
            payments.Where(p => p.Status == PaymentStatuses.Completed && p.PaidAt >= monthStart).Sum(p => p.Amount),
            payments.Where(p => p.Status == PaymentStatuses.Completed).Sum(p => p.Amount),
            payments.Count(p => p.Status == PaymentStatuses.Pending),
            pendingSerials);
    }

    public async Task<IReadOnlyList<ClientDto>> GetClientsAsync(string? search, CancellationToken ct = default)
    {
        var q = db.Clients.AsNoTracking().Include(c => c.Licenses).AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToLowerInvariant();
            q = q.Where(c =>
                c.CompanyName.ToLower().Contains(s) ||
                c.ContactName.ToLower().Contains(s) ||
                c.Email.ToLower().Contains(s) ||
                c.Phone.Contains(s));
        }

        var list = await q.OrderByDescending(c => c.CreatedAt).ToListAsync(ct);
        return await MapClientsAsync(list, ct);
    }

    public async Task<ClientDto?> GetClientAsync(int id, CancellationToken ct = default)
    {
        var client = await db.Clients.AsNoTracking()
            .Include(c => c.Licenses)
            .FirstOrDefaultAsync(c => c.Id == id, ct);
        if (client is null)
        {
            return null;
        }

        var mapped = await MapClientsAsync([client], ct);
        return mapped.FirstOrDefault();
    }

    public Task<(ClientDto? Dto, string? Error)> CreateClientAsync(ClientUpsertRequest request, CancellationToken ct = default)
        => Task.FromResult<(ClientDto?, string?)>((null,
            "Do not create clients here. Open Requests → Approve request. The client profile is created automatically."));

    public async Task<(ClientDto? Dto, string? Error)> UpdateClientAsync(int id, ClientUpsertRequest request, CancellationToken ct = default)
    {
        var error = ValidateClient(request);
        if (error is not null)
        {
            return (null, error);
        }

        var client = await db.Clients.Include(c => c.Licenses).FirstOrDefaultAsync(c => c.Id == id, ct);
        if (client is null)
        {
            return (null, "Client not found.");
        }

        var email = request.Email.Trim().ToLowerInvariant();
        if (await db.Clients.AnyAsync(c => c.Id != id && c.Email.ToLower() == email, ct))
        {
            return (null, "A client with this email already exists.");
        }

        client.CompanyName = request.CompanyName.Trim();
        client.ContactName = request.ContactName.Trim();
        client.Email = email;
        client.Phone = request.Phone.Trim();
        client.City = NullIfEmpty(request.City);
        client.Address = NullIfEmpty(request.Address);
        client.Notes = NullIfEmpty(request.Notes);
        client.Status = request.Status.Trim();
        client.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return (await GetClientAsync(id, ct), null);
    }

    public async Task<IReadOnlyList<LicenseDto>> GetLicensesAsync(string? search, int? clientId, CancellationToken ct = default)
    {
        // Keep one active license per client (fixes older Trial + Monthly duplicates).
        await ConsolidateDuplicateActiveLicensesAsync(ct);

        var q = db.Licenses.AsNoTracking()
            .Include(l => l.Client)
            .Include(l => l.Activations)
            .AsQueryable();

        if (clientId is > 0)
        {
            q = q.Where(l => l.ClientId == clientId);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToLowerInvariant();
            q = q.Where(l =>
                l.LicenseKey.ToLower().Contains(s) ||
                (l.Client != null && l.Client.CompanyName.ToLower().Contains(s)) ||
                l.PlanType.ToLower().Contains(s) ||
                l.Status.ToLower().Contains(s));
        }

        var list = await q.OrderByDescending(l => l.CreatedAt).ToListAsync(ct);
        foreach (var license in list)
        {
            if (license.Status == LicenseStatuses.Active &&
                license.PlanType != PlanTypes.Lifetime &&
                license.ExpiresAt.Date < DateTime.UtcNow.Date)
            {
                license.Status = LicenseStatuses.Expired;
            }
        }

        // Only active licenses, one per client (no Trial+Yearly duplicates).
        var onePerClient = list
            .Where(l => l.Status == LicenseStatuses.Active)
            .GroupBy(l => l.ClientId)
            .Select(g => g.OrderByDescending(l => l.CreatedAt).First())
            .OrderByDescending(l => l.CreatedAt)
            .ToList();

        return onePerClient.Select(MapLicense).ToList();
    }

    /// <summary>
    /// One client = one active license. Creating Trial/Monthly/Yearly updates the same license (does not add a second row).
    /// </summary>
    public async Task<(LicenseDto? Dto, string? Error)> CreateLicenseAsync(LicenseCreateRequest request, CancellationToken ct = default)
    {
        if (!ValidPlanTypes.Contains(request.PlanType))
        {
            return (null, "Invalid plan type.");
        }

        if (request.MaxMachines is < 1 or > 50)
        {
            return (null, "Max machines must be between 1 and 50.");
        }

        var client = await db.Clients.FirstOrDefaultAsync(c => c.Id == request.ClientId, ct);
        if (client is null)
        {
            return (null, "Client not found.");
        }

        var starts = DateTimeUtc.AsUtcDate(request.StartsAt ?? DateTimeUtc.UtcToday);
        var expires = DateTimeUtc.AsUtcDate(request.ExpiresAt ?? DefaultExpiry(request.PlanType, starts));
        if (request.PlanType != PlanTypes.Lifetime && expires < starts)
        {
            return (null, "Expiry date must be on or after start date.");
        }

        if (request.PlanType == PlanTypes.Lifetime)
        {
            expires = DateTimeUtc.AsUtcDate(starts.AddYears(100));
        }

        var license = await UpsertActiveLicenseAsync(
            client.Id, request.PlanType, request.MaxMachines, starts, expires, request.Notes, ct);

        var loaded = await db.Licenses.AsNoTracking()
            .Include(l => l.Client)
            .Include(l => l.Activations)
            .FirstAsync(l => l.Id == license.Id, ct);
        return (MapLicense(loaded), null);
    }

    /// <summary>Create or update the single active license for a client.</summary>
    public async Task<License> UpsertActiveLicenseAsync(
        int clientId,
        string planType,
        int maxMachines,
        DateTime starts,
        DateTime expires,
        string? notes,
        CancellationToken ct = default)
    {
        starts = DateTimeUtc.AsUtcDate(starts);
        expires = DateTimeUtc.AsUtcDate(expires);

        var existing = await db.Licenses
            .Where(l => l.ClientId == clientId)
            .OrderByDescending(l => l.Status == LicenseStatuses.Active)
            .ThenByDescending(l => l.CreatedAt)
            .ToListAsync(ct);

        var keep = existing.FirstOrDefault(l => l.Status == LicenseStatuses.Active) ?? existing.FirstOrDefault();

        foreach (var extra in existing.Where(l => keep is null || l.Id != keep.Id))
        {
            if (extra.Status == LicenseStatuses.Active)
            {
                extra.Status = LicenseStatuses.Revoked;
                extra.Notes = string.IsNullOrWhiteSpace(extra.Notes)
                    ? "Replaced by single-license policy"
                    : extra.Notes + " | Replaced";
            }
        }

        if (keep is null)
        {
            string key;
            do
            {
                key = LicenseKeyGenerator.Generate();
            } while (await db.Licenses.AnyAsync(l => l.LicenseKey == key, ct));

            keep = new License
            {
                ClientId = clientId,
                LicenseKey = key,
                PlanType = planType,
                MaxMachines = maxMachines,
                StartsAt = starts,
                ExpiresAt = expires,
                Status = LicenseStatuses.Active,
                Notes = NullIfEmpty(notes),
                CreatedAt = DateTime.UtcNow
            };
            db.Licenses.Add(keep);
        }
        else
        {
            keep.PlanType = planType;
            keep.MaxMachines = maxMachines;
            keep.StartsAt = starts;
            keep.ExpiresAt = expires;
            keep.Status = LicenseStatuses.Active;
            keep.UpdatedAt = DateTime.UtcNow;
            if (!string.IsNullOrWhiteSpace(notes))
            {
                keep.Notes = notes.Trim();
            }
        }

        await db.SaveChangesAsync(ct);
        return keep;
    }

    private async Task ConsolidateDuplicateActiveLicensesAsync(CancellationToken ct)
    {
        var groups = await db.Licenses
            .Where(l => l.Status == LicenseStatuses.Active)
            .GroupBy(l => l.ClientId)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToListAsync(ct);

        foreach (var clientId in groups)
        {
            var actives = await db.Licenses
                .Where(l => l.ClientId == clientId && l.Status == LicenseStatuses.Active)
                .OrderByDescending(l => l.CreatedAt)
                .ToListAsync(ct);

            foreach (var extra in actives.Skip(1))
            {
                extra.Status = LicenseStatuses.Revoked;
                extra.Notes = string.IsNullOrWhiteSpace(extra.Notes)
                    ? "Duplicate removed — one license per client"
                    : extra.Notes + " | Duplicate removed";
            }
        }

        if (groups.Count > 0)
        {
            await db.SaveChangesAsync(ct);
        }
    }

    public async Task<(LicenseDto? Dto, string? Error)> UpdateLicenseAsync(int id, LicenseUpdateRequest request, CancellationToken ct = default)
    {
        if (!ValidPlanTypes.Contains(request.PlanType))
        {
            return (null, "Invalid plan type.");
        }

        if (!ValidLicenseStatuses.Contains(request.Status))
        {
            return (null, "Invalid license status.");
        }

        if (request.MaxMachines is < 1 or > 50)
        {
            return (null, "Max machines must be between 1 and 50.");
        }

        var license = await db.Licenses
            .Include(l => l.Client)
            .Include(l => l.Activations)
            .FirstOrDefaultAsync(l => l.Id == id, ct);
        if (license is null)
        {
            return (null, "License not found.");
        }

        var starts = DateTimeUtc.AsUtcDate(request.StartsAt);
        var expires = DateTimeUtc.AsUtcDate(request.ExpiresAt);
        if (request.PlanType != PlanTypes.Lifetime && expires < starts)
        {
            return (null, "Expiry date must be on or after start date.");
        }

        license.PlanType = request.PlanType;
        license.MaxMachines = request.MaxMachines;
        license.StartsAt = starts;
        license.ExpiresAt = request.PlanType == PlanTypes.Lifetime
            ? DateTimeUtc.AsUtcDate(starts.AddYears(100))
            : expires;
        license.Status = request.Status;
        license.Notes = NullIfEmpty(request.Notes);
        license.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return (MapLicense(license), null);
    }

    public async Task<(LicenseDto? Dto, string? Error)> ExtendLicenseAsync(int id, ExtendLicenseRequest request, CancellationToken ct = default)
    {
        var license = await db.Licenses
            .Include(l => l.Client)
            .Include(l => l.Activations)
            .FirstOrDefaultAsync(l => l.Id == id, ct);
        if (license is null)
        {
            return (null, "License not found.");
        }

        if (license.PlanType == PlanTypes.Lifetime)
        {
            return (null, "Lifetime licenses cannot be extended.");
        }

        if (license.Status == LicenseStatuses.Revoked)
        {
            return (null, "Revoked licenses cannot be extended.");
        }

        var today = DateTimeUtc.UtcToday;
        var baseDate = DateTimeUtc.AsUtcDate(license.ExpiresAt) < today
            ? today
            : DateTimeUtc.AsUtcDate(license.ExpiresAt);
        license.ExpiresAt = DateTimeUtc.AsUtcDate(baseDate.AddDays(request.Days));
        license.Status = LicenseStatuses.Active;
        license.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return (MapLicense(license), null);
    }

    public async Task<(LicenseDto? Dto, string? Error)> RevokeLicenseAsync(int id, CancellationToken ct = default)
    {
        var license = await db.Licenses
            .Include(l => l.Client)
            .Include(l => l.Activations)
            .FirstOrDefaultAsync(l => l.Id == id, ct);
        if (license is null)
        {
            return (null, "License not found.");
        }

        license.Status = LicenseStatuses.Revoked;
        license.UpdatedAt = DateTime.UtcNow;
        foreach (var activation in license.Activations.Where(a => a.IsActive))
        {
            activation.IsActive = false;
            activation.DeactivatedAt = DateTime.UtcNow;
            activation.DeactivateReason = "License revoked";
        }

        await db.SaveChangesAsync(ct);
        return (MapLicense(license), null);
    }

    public async Task<IReadOnlyList<ActivationDto>> GetActivationsAsync(string? search, bool? activeOnly, CancellationToken ct = default)
    {
        var q = db.Activations.AsNoTracking()
            .Include(a => a.License)!.ThenInclude(l => l!.Client)
            .AsQueryable();

        if (activeOnly == true)
        {
            q = q.Where(a => a.IsActive);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToLowerInvariant();
            q = q.Where(a =>
                a.MachineId.ToLower().Contains(s) ||
                (a.MachineName != null && a.MachineName.ToLower().Contains(s)) ||
                (a.License != null && a.License.LicenseKey.ToLower().Contains(s)) ||
                (a.License != null && a.License.Client != null && a.License.Client.CompanyName.ToLower().Contains(s)));
        }

        var list = await q.OrderByDescending(a => a.ActivatedAt).ToListAsync(ct);
        return list.Select(MapActivation).ToList();
    }

    public async Task<(ActivationDto? Dto, string? Error)> DeactivateAsync(int id, DeactivateRequest request, CancellationToken ct = default)
    {
        var activation = await db.Activations
            .Include(a => a.License)!.ThenInclude(l => l!.Client)
            .FirstOrDefaultAsync(a => a.Id == id, ct);
        if (activation is null)
        {
            return (null, "Activation not found.");
        }

        if (!activation.IsActive)
        {
            return (MapActivation(activation), null);
        }

        activation.IsActive = false;
        activation.DeactivatedAt = DateTime.UtcNow;
        activation.DeactivateReason = NullIfEmpty(request.Reason) ?? "Deactivated by admin";
        await db.SaveChangesAsync(ct);
        return (MapActivation(activation), null);
    }

    public async Task<IReadOnlyList<PaymentDto>> GetPaymentsAsync(string? search, CancellationToken ct = default)
    {
        var q = db.Payments.AsNoTracking()
            .Include(p => p.Client)
            .Include(p => p.License)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToLowerInvariant();
            q = q.Where(p =>
                (p.Client != null && p.Client.CompanyName.ToLower().Contains(s)) ||
                (p.Reference != null && p.Reference.ToLower().Contains(s)) ||
                p.Method.ToLower().Contains(s) ||
                p.Status.ToLower().Contains(s) ||
                (p.License != null && p.License.LicenseKey.ToLower().Contains(s)));
        }

        var list = await q.OrderByDescending(p => p.PaidAt).ToListAsync(ct);
        return list.Select(MapPayment).ToList();
    }

    public async Task<(PaymentDto? Dto, string? Error)> CreatePaymentAsync(PaymentUpsertRequest request, CancellationToken ct = default)
    {
        var error = await ValidatePaymentAsync(request, ct);
        if (error is not null)
        {
            return (null, error);
        }

        var payment = new Payment
        {
            ClientId = request.ClientId,
            LicenseId = request.LicenseId,
            Amount = Math.Round(request.Amount, 2),
            Currency = request.Currency.Trim().ToUpperInvariant(),
            Method = request.Method.Trim(),
            Reference = NullIfEmpty(request.Reference),
            Status = request.Status.Trim(),
            PaidAt = request.PaidAt?.ToUniversalTime() ?? DateTime.UtcNow,
            Notes = NullIfEmpty(request.Notes),
            CreatedAt = DateTime.UtcNow
        };
        db.Payments.Add(payment);
        await db.SaveChangesAsync(ct);

        var loaded = await db.Payments.AsNoTracking()
            .Include(p => p.Client)
            .Include(p => p.License)
            .FirstAsync(p => p.Id == payment.Id, ct);
        return (MapPayment(loaded), null);
    }

    public async Task<(PaymentDto? Dto, string? Error)> UpdatePaymentAsync(int id, PaymentUpsertRequest request, CancellationToken ct = default)
    {
        var error = await ValidatePaymentAsync(request, ct);
        if (error is not null)
        {
            return (null, error);
        }

        var payment = await db.Payments
            .Include(p => p.Client)
            .Include(p => p.License)
            .FirstOrDefaultAsync(p => p.Id == id, ct);
        if (payment is null)
        {
            return (null, "Payment not found.");
        }

        payment.ClientId = request.ClientId;
        payment.LicenseId = request.LicenseId;
        payment.Amount = Math.Round(request.Amount, 2);
        payment.Currency = request.Currency.Trim().ToUpperInvariant();
        payment.Method = request.Method.Trim();
        payment.Reference = NullIfEmpty(request.Reference);
        payment.Status = request.Status.Trim();
        payment.PaidAt = request.PaidAt?.ToUniversalTime() ?? payment.PaidAt;
        payment.Notes = NullIfEmpty(request.Notes);
        await db.SaveChangesAsync(ct);
        return (MapPayment(payment), null);
    }

    private async Task<string?> ValidatePaymentAsync(PaymentUpsertRequest request, CancellationToken ct)
    {
        if (!ValidPaymentMethods.Contains(request.Method))
        {
            return "Invalid payment method.";
        }

        if (!ValidPaymentStatuses.Contains(request.Status))
        {
            return "Invalid payment status.";
        }

        if (request.Amount <= 0)
        {
            return "Amount must be greater than zero.";
        }

        if (!await db.Clients.AnyAsync(c => c.Id == request.ClientId, ct))
        {
            return "Client not found.";
        }

        if (request.LicenseId is int lid &&
            !await db.Licenses.AnyAsync(l => l.Id == lid && l.ClientId == request.ClientId, ct))
        {
            return "License not found for this client.";
        }

        return null;
    }

    private static string? ValidateClient(ClientUpsertRequest request)
    {
        if (!ValidClientStatuses.Contains(request.Status))
        {
            return "Invalid client status.";
        }

        if (string.IsNullOrWhiteSpace(request.CompanyName) || request.CompanyName.Trim().Length < 2)
        {
            return "Company name is required.";
        }

        if (string.IsNullOrWhiteSpace(request.ContactName) || request.ContactName.Trim().Length < 2)
        {
            return "Contact name is required.";
        }

        if (string.IsNullOrWhiteSpace(request.Email) || !request.Email.Contains('@'))
        {
            return "A valid email is required.";
        }

        var phone = request.Phone?.Trim() ?? string.Empty;
        if (phone.Length < 8 || !phone.All(c => char.IsDigit(c) || c is '+' or '-' or ' '))
        {
            return "Enter a valid phone number.";
        }

        return null;
    }

    private static DateTime DefaultExpiry(string planType, DateTime starts) => planType switch
    {
        PlanTypes.Trial => starts.AddDays(14),
        PlanTypes.Monthly => starts.AddMonths(1),
        PlanTypes.Yearly => starts.AddYears(1),
        PlanTypes.Lifetime => starts.AddYears(100),
        _ => starts.AddMonths(1)
    };

    public async Task<(ClientDto? Dto, string? Error)> DeactivateClientAsync(int id, CancellationToken ct = default)
    {
        var client = await db.Clients.Include(c => c.Licenses).FirstOrDefaultAsync(c => c.Id == id, ct);
        if (client is null)
        {
            return (null, "Client not found.");
        }

        client.Status = ClientStatuses.Deactivated;
        client.UpdatedAt = DateTime.UtcNow;

        foreach (var license in client.Licenses.Where(l => l.Status == LicenseStatuses.Active))
        {
            license.Status = LicenseStatuses.Revoked;
            license.UpdatedAt = DateTime.UtcNow;
            license.Notes = string.IsNullOrWhiteSpace(license.Notes)
                ? "Deactivated with client"
                : license.Notes + " | Deactivated";
        }

        var licenseIds = client.Licenses.Select(l => l.Id).ToList();
        foreach (var activation in await db.Activations.Where(a => licenseIds.Contains(a.LicenseId) && a.IsActive).ToListAsync(ct))
        {
            activation.IsActive = false;
            activation.DeactivatedAt = DateTime.UtcNow;
            activation.DeactivateReason = "Client deactivated";
        }

        foreach (var serial in await db.SerialRegistrations
                     .Where(s => s.ClientId == id && s.Status == SerialStatuses.Approved)
                     .ToListAsync(ct))
        {
            serial.Status = SerialStatuses.Deactivated;
            serial.ReviewedAt = DateTime.UtcNow;
            serial.ReviewNotes = "Deactivated by Super Admin";
        }

        foreach (var user in await db.ClientUsers.Where(u => u.ClientId == id).ToListAsync(ct))
        {
            user.IsActive = false;
        }

        await ClientAccessGuard.RevokeSessionsForClientAsync(db, id, ct);

        await db.SaveChangesAsync(ct);
        return (await GetClientAsync(id, ct), null);
    }

    /// <summary>Full re-activate: client + login + license + all PC serials for this client.</summary>
    public async Task<(ClientDto? Dto, string? Error)> ReactivateClientAsync(int id, CancellationToken ct = default)
    {
        var tempPassword = PortalCredentials.DefaultPassword;
        var client = await db.Clients.Include(c => c.Licenses).FirstOrDefaultAsync(c => c.Id == id, ct);
        if (client is null)
        {
            return (null, "Client not found.");
        }

        client.Status = ClientStatuses.Active;
        client.UpdatedAt = DateTime.UtcNow;

        var (hash, salt) = PasswordHasher.HashPassword(tempPassword);
        var users = await db.ClientUsers.Where(u => u.ClientId == id).ToListAsync(ct);
        if (users.Count == 0)
        {
            var username = await PortalCredentials.AllocateNextLoginIdAsync(db, ct);
            db.ClientUsers.Add(new ClientUser
            {
                ClientId = client.Id,
                Username = username,
                PasswordHash = hash,
                PasswordSalt = salt,
                PortalPassword = tempPassword,
                DisplayName = client.CompanyName,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            });
        }
        else
        {
            foreach (var user in users)
            {
                user.IsActive = true;
                user.PasswordHash = hash;
                user.PasswordSalt = salt;
                user.PortalPassword = tempPassword;
                user.DisplayName = client.CompanyName;
            }
        }

        var plan = client.Licenses
            .OrderByDescending(l => l.CreatedAt)
            .Select(l => l.PlanType)
            .FirstOrDefault() ?? PlanTypes.Yearly;

        var starts = DateTimeUtc.UtcToday;
        var expires = DateTimeUtc.AsUtcDate(plan switch
        {
            PlanTypes.Trial => starts.AddDays(14),
            PlanTypes.Monthly => starts.AddMonths(1),
            PlanTypes.Lifetime => starts.AddYears(100),
            _ => starts.AddYears(1)
        });

        var license = await UpsertActiveLicenseAsync(
            client.Id, plan, 1, starts, expires, "Re-activated by Super Admin", ct);

        foreach (var serial in await db.SerialRegistrations
                     .Where(s => s.ClientId == id &&
                                 (s.Status == SerialStatuses.Deactivated ||
                                  s.Status == SerialStatuses.Revoked ||
                                  s.Status == SerialStatuses.Approved))
                     .ToListAsync(ct))
        {
            serial.Status = SerialStatuses.Approved;
            serial.LicenseId = license.Id;
            serial.ClientId = client.Id;
            serial.ReviewedAt = DateTime.UtcNow;
            serial.ReviewNotes = "Re-activated by Super Admin";
        }

        await db.SaveChangesAsync(ct);
        return (await GetClientAsync(id, ct), null);
    }

    /// <summary>Deletes all business data. Keeps only Super Admin login.</summary>
    public async Task<(string? Message, string? Error)> FactoryResetAsync(CancellationToken ct = default)
    {
        await db.ClientSessions.ExecuteDeleteAsync(ct);
        await db.ClientUsers.ExecuteDeleteAsync(ct);
        await db.Activations.ExecuteDeleteAsync(ct);
        await db.Payments.ExecuteDeleteAsync(ct);
        await db.SerialRegistrations.ExecuteDeleteAsync(ct);
        await db.Licenses.ExecuteDeleteAsync(ct);
        await db.Clients.ExecuteDeleteAsync(ct);
        await db.AdminSessions.ExecuteDeleteAsync(ct);
        // Keep AdminSerials so Super Admin PCs stay authorized after cleanup.

        return (
            "Full cleanup done. Only Super Admin remains.\nYour Super Admin username/password and authorized laptops are unchanged.",
            null);
    }

    public async Task<IReadOnlyList<AdminSerialDto>> GetAdminSerialsAsync(CancellationToken ct = default)
    {
        var list = await db.AdminSerials.AsNoTracking()
            .OrderByDescending(s => s.IsActive)
            .ThenByDescending(s => s.CreatedAt)
            .ToListAsync(ct);
        return list.Select(MapAdminSerial).ToList();
    }

    public async Task<(AdminSerialDto? Dto, string? Error)> AddAdminSerialAsync(
        AdminSerialUpsertRequest request,
        CancellationToken ct = default)
    {
        var serial = SerialFormatter.Normalize(request.SerialNumber);
        if (serial.Length < 8)
        {
            return (null, "Enter a valid laptop serial number (use Copy serial on that PC's login screen).");
        }

        // Prefer exact match; also merge older truncated / RX-prefixed duplicates.
        var existing = await db.AdminSerials.FirstOrDefaultAsync(s => s.SerialNumber == serial, ct);
        existing ??= await db.AdminSerials.FirstOrDefaultAsync(s =>
            s.SerialNumber == "RX" + serial ||
            serial.StartsWith(s.SerialNumber) ||
            s.SerialNumber.StartsWith(serial), ct);

        if (existing is not null)
        {
            existing.SerialNumber = serial; // repair bad/truncated stored values
            existing.IsActive = true;
            existing.MachineName = string.IsNullOrWhiteSpace(request.MachineName)
                ? existing.MachineName
                : request.MachineName.Trim();
            existing.Notes = string.IsNullOrWhiteSpace(request.Notes) ? existing.Notes : request.Notes.Trim();
            await db.SaveChangesAsync(ct);
            return (MapAdminSerial(existing), null);
        }

        var entity = new AdminSerial
        {
            SerialNumber = serial,
            MachineName = string.IsNullOrWhiteSpace(request.MachineName) ? null : request.MachineName.Trim(),
            Notes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim(),
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        db.AdminSerials.Add(entity);
        await db.SaveChangesAsync(ct);
        return (MapAdminSerial(entity), null);
    }

    public async Task<(string? Message, string? Error)> RemoveAdminSerialAsync(int id, CancellationToken ct = default)
    {
        var entity = await db.AdminSerials.FirstOrDefaultAsync(s => s.Id == id, ct);
        if (entity is null)
        {
            return (null, "Admin laptop serial not found.");
        }

        var activeCount = await db.AdminSerials.CountAsync(s => s.IsActive, ct);
        if (entity.IsActive && activeCount <= 1)
        {
            return (null, "Cannot remove the last Super Admin laptop. Add another serial first.");
        }

        db.AdminSerials.Remove(entity);
        await db.SaveChangesAsync(ct);
        return ("Super Admin laptop removed. That PC can no longer sign in as Super Admin.", null);
    }

    private static AdminSerialDto MapAdminSerial(AdminSerial s) =>
        new(s.Id, s.SerialNumber, SerialFormatter.Format(s.SerialNumber), s.MachineName, s.Notes,
            s.IsActive, s.CreatedAt, s.LastLoginAt);

    private async Task<string> EnsurePortalUserAsync(Client client, CancellationToken ct)
    {
        var existing = await db.ClientUsers.FirstOrDefaultAsync(u => u.ClientId == client.Id, ct);
        if (existing is not null)
        {
            return existing.Username;
        }

        var username = await PortalCredentials.AllocateNextLoginIdAsync(db, ct);
        var password = PortalCredentials.DefaultPassword;
        var (hash, salt) = PasswordHasher.HashPassword(password);
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

    private async Task<IReadOnlyList<ClientDto>> MapClientsAsync(IReadOnlyList<Client> clients, CancellationToken ct)
    {
        if (clients.Count == 0)
        {
            return [];
        }

        var ids = clients.Select(c => c.Id).ToList();
        var users = await db.ClientUsers.AsNoTracking()
            .Where(u => ids.Contains(u.ClientId))
            .ToListAsync(ct);
        var byClient = users
            .GroupBy(u => u.ClientId)
            .ToDictionary(g => g.Key, g => g.OrderByDescending(u => u.IsActive).ThenBy(u => u.Id).First());

        return clients.Select(c =>
        {
            byClient.TryGetValue(c.Id, out var user);
            return new ClientDto(
                c.Id, c.CompanyName, c.ContactName, c.Email, c.Phone, c.City, c.Address, c.Notes, c.Status,
                c.CreatedAt, c.Licenses.Count, c.Licenses.Count(l => l.Status == LicenseStatuses.Active),
                user?.Username,
                string.IsNullOrWhiteSpace(user?.PortalPassword) ? PortalCredentials.DefaultPassword : user!.PortalPassword);
        }).ToList();
    }

    private static LicenseDto MapLicense(License l)
    {
        var days = l.PlanType == PlanTypes.Lifetime
            ? 36500
            : Math.Max(0, (l.ExpiresAt.Date - DateTime.UtcNow.Date).Days);
        return new LicenseDto(
            l.Id, l.ClientId, l.Client?.CompanyName ?? string.Empty, l.LicenseKey, l.PlanType,
            l.MaxMachines, l.Activations.Count(a => a.IsActive), l.StartsAt, l.ExpiresAt, l.Status,
            l.Notes, l.CreatedAt, days);
    }

    private static ActivationDto MapActivation(Activation a) =>
        new(a.Id, a.LicenseId, a.License?.LicenseKey ?? string.Empty, a.License?.Client?.CompanyName ?? string.Empty,
            a.MachineId, a.MachineName, a.OsInfo, a.ActivatedAt, a.LastSeenAt, a.IsActive,
            a.DeactivatedAt, a.DeactivateReason);

    private static PaymentDto MapPayment(Payment p) =>
        new(p.Id, p.ClientId, p.Client?.CompanyName ?? string.Empty, p.LicenseId, p.License?.LicenseKey,
            p.Amount, p.Currency, p.Method, p.Reference, p.Status, p.PaidAt, p.Notes, p.CreatedAt);

    private static string? NullIfEmpty(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
