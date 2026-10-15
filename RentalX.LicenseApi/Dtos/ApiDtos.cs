using System.ComponentModel.DataAnnotations;

namespace RentalX.LicenseApi.Dtos;

public record LoginRequest(
    [property: Required, MinLength(3)] string Username,
    [property: Required, MinLength(6)] string Password,
    string? SerialNumber = null);

public record LoginResponse(
    string Token,
    string DisplayName,
    DateTime ExpiresAt,
    string Role,
    int? ClientId = null,
    string? CompanyName = null,
    string? SerialNumber = null,
    string? Username = null,
    string? ContactName = null,
    string? Email = null,
    string? Phone = null,
    string? Address = null,
    string? City = null);

public record ChangeAdminCredentialsRequest(
    [property: Required, MinLength(6)] string CurrentPassword,
    [property: Required, MinLength(3)] string NewUsername,
    [property: Required, MinLength(6)] string NewPassword);

public record ChangeClientPasswordRequest(
    [property: Required, MinLength(6)] string CurrentPassword,
    [property: Required, MinLength(6)] string NewPassword);

public record AdminSerialDto(
    int Id,
    string SerialNumber,
    string DisplaySerial,
    string? MachineName,
    string? Notes,
    bool IsActive,
    DateTime CreatedAt,
    DateTime? LastLoginAt);

public record AdminSerialUpsertRequest(
    [property: Required, MinLength(8)] string SerialNumber,
    [property: MaxLength(120)] string? MachineName = null,
    [property: MaxLength(250)] string? Notes = null);

public record DashboardStatsDto(
    int TotalClients,
    int ActiveClients,
    int ActiveLicenses,
    int ExpiringSoon,
    int ActiveActivations,
    decimal RevenueThisMonth,
    decimal RevenueTotal,
    int PendingPayments,
    int PendingSerials);

public record SerialDto(
    int Id,
    string SerialNumber,
    string DisplaySerial,
    string? MachineName,
    string CompanyName,
    string ContactName,
    string Email,
    string Phone,
    string? Address,
    string? Pincode,
    string? Notes,
    string Status,
    int? ClientId,
    string? ClientName,
    int? LicenseId,
    string? LicenseKey,
    DateTime AppliedAt,
    DateTime? ReviewedAt,
    string? ReviewNotes);

public record SerialApplyRequest(
    [property: Required, MinLength(8)] string SerialNumber,
    [property: MaxLength(120)] string? MachineName,
    [property: Required, MinLength(2), MaxLength(120)] string CompanyName,
    [property: Required, EmailAddress, MaxLength(120)] string Email,
    [property: Required, MinLength(8), MaxLength(20)] string Phone,
    [property: Required, MinLength(5), MaxLength(250)] string Address,
    [property: Required, MinLength(4), MaxLength(12)] string Pincode,
    [property: MaxLength(500)] string? Notes);

public record SerialApproveRequest(
    [property: Required, Range(1, int.MaxValue)] int ClientId,
    int? LicenseId,
    [property: MaxLength(500)] string? ReviewNotes);

/// <summary>One-click: create client + login + license + approve serial.</summary>
public record QuickApproveResponse(
    SerialDto Serial,
    string LoginId,
    string TemporaryPassword,
    string LicenseKey,
    string Message);

public record SerialRejectRequest([property: MaxLength(500)] string? ReviewNotes);

public record QuickApproveRequest(
    [property: MaxLength(500)] string? ReviewNotes,
    [property: MaxLength(20)] string? PlanType);

public record SerialConfigureRequest(
    [property: Required, MinLength(8)] string SerialNumber,
    [property: MaxLength(120)] string? MachineName,
    [property: Required, Range(1, int.MaxValue)] int ClientId,
    int? LicenseId,
    [property: MaxLength(500)] string? ReviewNotes);

public record ClientDto(
    int Id,
    string CompanyName,
    string ContactName,
    string Email,
    string Phone,
    string? City,
    string? Address,
    string? Notes,
    string Status,
    DateTime CreatedAt,
    int LicenseCount,
    int ActiveLicenseCount,
    string? PortalUsername = null,
    string? PortalPassword = null);

public record ClientUpsertRequest(
    [property: Required, MinLength(2), MaxLength(120)] string CompanyName,
    [property: Required, MinLength(2), MaxLength(80)] string ContactName,
    [property: Required, EmailAddress, MaxLength(120)] string Email,
    [property: Required, MinLength(8), MaxLength(20)] string Phone,
    [property: MaxLength(80)] string? City,
    [property: MaxLength(250)] string? Address,
    [property: MaxLength(500)] string? Notes,
    [property: Required] string Status);

public record LicenseDto(
    int Id,
    int ClientId,
    string ClientName,
    string LicenseKey,
    string PlanType,
    int MaxMachines,
    int ActiveMachineCount,
    DateTime StartsAt,
    DateTime ExpiresAt,
    string Status,
    string? Notes,
    DateTime CreatedAt,
    int DaysRemaining);

public record LicenseCreateRequest(
    [property: Required, Range(1, int.MaxValue)] int ClientId,
    [property: Required] string PlanType,
    [property: Range(1, 50)] int MaxMachines,
    DateTime? StartsAt,
    DateTime? ExpiresAt,
    [property: MaxLength(500)] string? Notes);

public record LicenseUpdateRequest(
    [property: Required] string PlanType,
    [property: Range(1, 50)] int MaxMachines,
    DateTime StartsAt,
    DateTime ExpiresAt,
    [property: Required] string Status,
    [property: MaxLength(500)] string? Notes);

public record ExtendLicenseRequest([property: Range(1, 3650)] int Days);

public record ActivationDto(
    int Id,
    int LicenseId,
    string LicenseKey,
    string ClientName,
    string MachineId,
    string? MachineName,
    string? OsInfo,
    DateTime ActivatedAt,
    DateTime LastSeenAt,
    bool IsActive,
    DateTime? DeactivatedAt,
    string? DeactivateReason);

public record DeactivateRequest([property: MaxLength(200)] string? Reason);

public record PaymentDto(
    int Id,
    int ClientId,
    string ClientName,
    int? LicenseId,
    string? LicenseKey,
    decimal Amount,
    string Currency,
    string Method,
    string? Reference,
    string Status,
    DateTime PaidAt,
    string? Notes,
    DateTime CreatedAt);

public record PaymentUpsertRequest(
    [property: Required, Range(1, int.MaxValue)] int ClientId,
    int? LicenseId,
    [property: Range(0.01, 10_000_000)] decimal Amount,
    [property: Required, MaxLength(8)] string Currency,
    [property: Required] string Method,
    [property: MaxLength(80)] string? Reference,
    [property: Required] string Status,
    DateTime? PaidAt,
    [property: MaxLength(500)] string? Notes);

public record ActivateRequest(
    [property: Required, MinLength(8)] string LicenseKey,
    [property: Required, MinLength(8), MaxLength(128)] string MachineId,
    [property: MaxLength(120)] string? MachineName,
    [property: MaxLength(120)] string? OsInfo);

public record LicenseCheckRequest(
    [property: Required, MinLength(8)] string LicenseKey,
    [property: Required, MinLength(8), MaxLength(128)] string MachineId);

public record LicenseStatusResponse(
    bool IsValid,
    string Message,
    string? CompanyName,
    string? PlanType,
    DateTime? ExpiresAt,
    int? DaysRemaining,
    int? MaxMachines,
    string? Status);
