using RentalX.Admin.Models;

namespace RentalX.Admin.Services;

public interface ILicenseApiClient
{
    string BaseUrl { get; set; }
    Task<(LoginResponse? Data, string? Error)> LoginAsync(LoginRequest request, CancellationToken ct = default);
    Task LogoutAsync(CancellationToken ct = default);
    Task<(DashboardStats? Data, string? Error)> GetDashboardAsync(CancellationToken ct = default);
    Task<(DbHealthResponse? Data, string? Error)> GetDbHealthAsync(CancellationToken ct = default);
    Task<(List<ClientDto>? Data, string? Error)> GetClientsAsync(string? search = null, CancellationToken ct = default);
    Task<(ClientDto? Data, string? Error)> CreateClientAsync(ClientUpsertRequest request, CancellationToken ct = default);
    Task<(ClientDto? Data, string? Error)> UpdateClientAsync(int id, ClientUpsertRequest request, CancellationToken ct = default);
    Task<(ClientDto? Data, string? Error)> DeactivateClientAsync(int id, CancellationToken ct = default);
    Task<(ClientDto? Data, string? Error)> ReactivateClientAsync(int id, CancellationToken ct = default);
    Task<(string? Message, string? Error)> FactoryResetAsync(CancellationToken ct = default);
    Task<(string? Username, string? Error)> ChangeAdminCredentialsAsync(ChangeAdminCredentialsRequest request, CancellationToken ct = default);
    /// <summary>Client password change using the client bearer token (Login ID stays the same).</summary>
    Task<(string? Message, string? Error)> ChangeClientPasswordAsync(
        ChangeClientPasswordRequest request,
        string? bearerToken,
        CancellationToken ct = default);
    Task<(List<AdminSerialDto>? Data, string? Error)> GetAdminSerialsAsync(CancellationToken ct = default);
    Task<(AdminSerialDto? Data, string? Error)> AddAdminSerialAsync(AdminSerialUpsertRequest request, CancellationToken ct = default);
    Task<(string? Message, string? Error)> RemoveAdminSerialAsync(int id, CancellationToken ct = default);
    Task<(List<LicenseDto>? Data, string? Error)> GetLicensesAsync(string? search = null, int? clientId = null, CancellationToken ct = default);
    Task<(LicenseDto? Data, string? Error)> CreateLicenseAsync(LicenseCreateRequest request, CancellationToken ct = default);
    Task<(LicenseDto? Data, string? Error)> UpdateLicenseAsync(int id, LicenseUpdateRequest request, CancellationToken ct = default);
    Task<(LicenseDto? Data, string? Error)> ExtendLicenseAsync(int id, int days, CancellationToken ct = default);
    Task<(LicenseDto? Data, string? Error)> RevokeLicenseAsync(int id, CancellationToken ct = default);
    Task<(List<ActivationDto>? Data, string? Error)> GetActivationsAsync(string? search = null, bool? activeOnly = null, CancellationToken ct = default);
    Task<(ActivationDto? Data, string? Error)> DeactivateAsync(int id, string? reason, CancellationToken ct = default);
    Task<(List<PaymentDto>? Data, string? Error)> GetPaymentsAsync(string? search = null, CancellationToken ct = default);
    Task<(PaymentDto? Data, string? Error)> CreatePaymentAsync(PaymentUpsertRequest request, CancellationToken ct = default);
    Task<(PaymentDto? Data, string? Error)> UpdatePaymentAsync(int id, PaymentUpsertRequest request, CancellationToken ct = default);
    Task<(SerialApplyResponse? Data, string? Error)> ApplySerialAsync(SerialApplyRequest request, CancellationToken ct = default);
    Task<(SerialApplyResponse? Data, string? Error)> GetSerialStatusAsync(string serialNumber, CancellationToken ct = default);
    Task<(List<SerialDto>? Data, string? Error)> GetSerialsAsync(string? search = null, string? status = null, CancellationToken ct = default);
    Task<(SerialDto? Data, string? Error)> ApproveSerialAsync(int id, SerialApproveRequest request, CancellationToken ct = default);
    Task<(QuickApproveResponse? Data, string? Error)> QuickApproveSerialAsync(int id, string? notes = null, string? planType = null, CancellationToken ct = default);
    Task<(SerialDto? Data, string? Error)> RejectSerialAsync(int id, string? reason, CancellationToken ct = default);
    Task<(SerialDto? Data, string? Error)> RevokeSerialAsync(int id, string? reason, CancellationToken ct = default);
    Task<(SerialDto? Data, string? Error)> ConfigureSerialAsync(SerialConfigureRequest request, CancellationToken ct = default);
}
