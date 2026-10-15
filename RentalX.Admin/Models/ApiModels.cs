namespace RentalX.Admin.Models;

public sealed class LoginRequest
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string? SerialNumber { get; set; }
}

public sealed class LoginResponse
{
    public string Token { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public string Role { get; set; } = string.Empty;
    public int? ClientId { get; set; }
    public string? CompanyName { get; set; }
    public string? SerialNumber { get; set; }
    public string? Username { get; set; }
    public string? ContactName { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Address { get; set; }
    public string? City { get; set; }
}

public sealed class ChangeAdminCredentialsRequest
{
    public string CurrentPassword { get; set; } = string.Empty;
    public string NewUsername { get; set; } = string.Empty;
    public string NewPassword { get; set; } = string.Empty;
}

public sealed class ChangeAdminCredentialsResponse
{
    public string? Username { get; set; }
    public string? Message { get; set; }
}

public sealed class ChangeClientPasswordRequest
{
    public string CurrentPassword { get; set; } = string.Empty;
    public string NewPassword { get; set; } = string.Empty;
}

public sealed class ChangeClientPasswordResponse
{
    public string? Username { get; set; }
    public string? Message { get; set; }
}

public sealed class AdminSerialDto
{
    public int Id { get; set; }
    public string SerialNumber { get; set; } = string.Empty;
    public string DisplaySerial { get; set; } = string.Empty;
    public string? MachineName { get; set; }
    public string? Notes { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }
}

public sealed class AdminSerialUpsertRequest
{
    public string SerialNumber { get; set; } = string.Empty;
    public string? MachineName { get; set; }
    public string? Notes { get; set; }
}

public static class UserRoles
{
    public const string SuperAdmin = "SuperAdmin";
    public const string Client = "Client";
}

public sealed class DashboardStats
{
    public int TotalClients { get; set; }
    public int ActiveClients { get; set; }
    public int ActiveLicenses { get; set; }
    public int ExpiringSoon { get; set; }
    public int ActiveActivations { get; set; }
    public decimal RevenueThisMonth { get; set; }
    public decimal RevenueTotal { get; set; }
    public int PendingPayments { get; set; }
    public int PendingSerials { get; set; }
}

public sealed class SerialDto
{
    public int Id { get; set; }
    public string SerialNumber { get; set; } = string.Empty;
    public string DisplaySerial { get; set; } = string.Empty;
    public string? MachineName { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public string ContactName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string? Address { get; set; }
    public string? Pincode { get; set; }
    public string? Notes { get; set; }
    public string Status { get; set; } = "Pending";
    public int? ClientId { get; set; }
    public string? ClientName { get; set; }
    public int? LicenseId { get; set; }
    public string? LicenseKey { get; set; }
    public DateTime AppliedAt { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public string? ReviewNotes { get; set; }
}

public sealed class SerialApproveRequest
{
    public int ClientId { get; set; }
    public int? LicenseId { get; set; }
    public string? ReviewNotes { get; set; }
}

public sealed class QuickApproveResponse
{
    public SerialDto Serial { get; set; } = new();
    public string LoginId { get; set; } = string.Empty;
    public string TemporaryPassword { get; set; } = string.Empty;
    public string LicenseKey { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}

public sealed class QuickApproveRequest
{
    public string? ReviewNotes { get; set; }
    public string? PlanType { get; set; }
}

public sealed class SerialRejectRequest
{
    public string? ReviewNotes { get; set; }
}

public sealed class SerialConfigureRequest
{
    public string SerialNumber { get; set; } = string.Empty;
    public string? MachineName { get; set; }
    public int ClientId { get; set; }
    public int? LicenseId { get; set; }
    public string? ReviewNotes { get; set; }
}

public sealed class SerialApplyRequest
{
    public string SerialNumber { get; set; } = string.Empty;
    public string? MachineName { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string Pincode { get; set; } = string.Empty;
    public string? Notes { get; set; }
}

public sealed class SerialApplyResponse
{
    public SerialDto? Data { get; set; }
    public string? Message { get; set; }
}

public sealed class ClientDto
{
    public int Id { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public string ContactName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string? City { get; set; }
    public string? Address { get; set; }
    public string? Notes { get; set; }
    public string Status { get; set; } = "Active";
    public DateTime CreatedAt { get; set; }
    public int LicenseCount { get; set; }
    public int ActiveLicenseCount { get; set; }
    public string? PortalUsername { get; set; }
    public string? PortalPassword { get; set; }
}

public sealed class ClientUpsertRequest
{
    public string CompanyName { get; set; } = string.Empty;
    public string ContactName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string? City { get; set; }
    public string? Address { get; set; }
    public string? Notes { get; set; }
    public string Status { get; set; } = "Active";
}

public sealed class LicenseDto
{
    public int Id { get; set; }
    public int ClientId { get; set; }
    public string ClientName { get; set; } = string.Empty;
    public string LicenseKey { get; set; } = string.Empty;
    public string PlanType { get; set; } = "Monthly";
    public int MaxMachines { get; set; }
    public int ActiveMachineCount { get; set; }
    public DateTime StartsAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public string Status { get; set; } = "Active";
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
    public int DaysRemaining { get; set; }
}

public sealed class LicenseCreateRequest
{
    public int ClientId { get; set; }
    public string PlanType { get; set; } = "Monthly";
    public int MaxMachines { get; set; } = 1;
    public DateTime? StartsAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public string? Notes { get; set; }
}

public sealed class LicenseUpdateRequest
{
    public string PlanType { get; set; } = "Monthly";
    public int MaxMachines { get; set; } = 1;
    public DateTime StartsAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public string Status { get; set; } = "Active";
    public string? Notes { get; set; }
}

public sealed class ExtendLicenseRequest
{
    public int Days { get; set; } = 30;
}

public sealed class ActivationDto
{
    public int Id { get; set; }
    public int LicenseId { get; set; }
    public string LicenseKey { get; set; } = string.Empty;
    public string ClientName { get; set; } = string.Empty;
    public string MachineId { get; set; } = string.Empty;
    public string? MachineName { get; set; }
    public string? OsInfo { get; set; }
    public DateTime ActivatedAt { get; set; }
    public DateTime LastSeenAt { get; set; }
    public bool IsActive { get; set; }
    public DateTime? DeactivatedAt { get; set; }
    public string? DeactivateReason { get; set; }
}

public sealed class DeactivateRequest
{
    public string? Reason { get; set; }
}

public sealed class PaymentDto
{
    public int Id { get; set; }
    public int ClientId { get; set; }
    public string ClientName { get; set; } = string.Empty;
    public int? LicenseId { get; set; }
    public string? LicenseKey { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "INR";
    public string Method { get; set; } = "Manual";
    public string? Reference { get; set; }
    public string Status { get; set; } = "Completed";
    public DateTime PaidAt { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
}

public sealed class PaymentUpsertRequest
{
    public int ClientId { get; set; }
    public int? LicenseId { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "INR";
    public string Method { get; set; } = "Manual";
    public string? Reference { get; set; }
    public string Status { get; set; } = "Completed";
    public DateTime? PaidAt { get; set; }
    public string? Notes { get; set; }
}

public sealed class MessageResponse
{
    public string? Message { get; set; }
}

public sealed class DbHealthResponse
{
    public bool Ok { get; set; }
    public string? Provider { get; set; }
    public string? DatabaseLabel { get; set; }
    public string? Message { get; set; }
    public int Clients { get; set; }
    public int Serials { get; set; }
    public int Licenses { get; set; }
}

public sealed class ApiError
{
    public string? Message { get; set; }
}

public sealed class NavItem
{
    public string Key { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string Icon { get; init; } = string.Empty;
}
