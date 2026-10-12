using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using RentalX.Admin.Models;

namespace RentalX.Admin.Services;

public sealed class LicenseApiClient : ILicenseApiClient
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly HttpClient _http;
    private readonly IAdminSession _session;
    private string _baseUrl = LicenseApiDefaults.BaseUrl;

    public LicenseApiClient(HttpClient http, IAdminSession session)
    {
        _http = http;
        _session = session;
        // Never set HttpClient.BaseAddress — it cannot change after the first request,
        // and this client may share the HttpClient instance with other services.
    }

    public string BaseUrl
    {
        get => _baseUrl;
        set
        {
            var trimmed = (value ?? string.Empty).Trim().TrimEnd('/');
            _baseUrl = string.IsNullOrWhiteSpace(trimmed) ? LicenseApiDefaults.BaseUrl : trimmed;
        }
    }

    public Task<(LoginResponse? Data, string? Error)> LoginAsync(LoginRequest request, CancellationToken ct = default) =>
        SendAsync<LoginResponse>(HttpMethod.Post, "api/auth/login", request, auth: false, ct);

    public async Task LogoutAsync(CancellationToken ct = default)
    {
        try
        {
            await SendAsync<object>(HttpMethod.Post, "api/auth/logout", null, auth: true, ct);
        }
        catch
        {
            // ignore network errors on logout
        }
    }

    public Task<(DashboardStats? Data, string? Error)> GetDashboardAsync(CancellationToken ct = default) =>
        SendAsync<DashboardStats>(HttpMethod.Get, "api/dashboard/stats", null, true, ct);

    public Task<(DbHealthResponse? Data, string? Error)> GetDbHealthAsync(CancellationToken ct = default) =>
        SendAsync<DbHealthResponse>(HttpMethod.Get, "api/health/db", null, auth: false, ct);

    public Task<(List<ClientDto>? Data, string? Error)> GetClientsAsync(string? search = null, CancellationToken ct = default) =>
        SendAsync<List<ClientDto>>(HttpMethod.Get, Query("api/clients", ("search", search)), null, true, ct);

    public Task<(ClientDto? Data, string? Error)> CreateClientAsync(ClientUpsertRequest request, CancellationToken ct = default) =>
        SendAsync<ClientDto>(HttpMethod.Post, "api/clients", request, true, ct);

    public Task<(ClientDto? Data, string? Error)> UpdateClientAsync(int id, ClientUpsertRequest request, CancellationToken ct = default) =>
        SendAsync<ClientDto>(HttpMethod.Put, $"api/clients/{id}", request, true, ct);

    public Task<(ClientDto? Data, string? Error)> DeactivateClientAsync(int id, CancellationToken ct = default) =>
        SendAsync<ClientDto>(HttpMethod.Post, $"api/clients/{id}/deactivate", null, true, ct);

    public Task<(ClientDto? Data, string? Error)> ReactivateClientAsync(int id, CancellationToken ct = default) =>
        SendAsync<ClientDto>(HttpMethod.Post, $"api/clients/{id}/reactivate", null, true, ct);

    public async Task<(string? Message, string? Error)> FactoryResetAsync(CancellationToken ct = default)
    {
        var (data, error) = await SendAsync<MessageResponse>(HttpMethod.Post, "api/admin/factory-reset", null, true, ct);
        return (data?.Message, error);
    }

    public async Task<(string? Username, string? Error)> ChangeAdminCredentialsAsync(
        ChangeAdminCredentialsRequest request,
        CancellationToken ct = default)
    {
        var (data, error) = await SendAsync<ChangeAdminCredentialsResponse>(
            HttpMethod.Put, "api/admin/credentials", request, true, ct);
        return (data?.Username, error);
    }

    public Task<(List<AdminSerialDto>? Data, string? Error)> GetAdminSerialsAsync(CancellationToken ct = default) =>
        SendAsync<List<AdminSerialDto>>(HttpMethod.Get, "api/admin/serials", null, true, ct);

    public Task<(AdminSerialDto? Data, string? Error)> AddAdminSerialAsync(
        AdminSerialUpsertRequest request,
        CancellationToken ct = default) =>
        SendAsync<AdminSerialDto>(HttpMethod.Post, "api/admin/serials", request, true, ct);

    public async Task<(string? Message, string? Error)> RemoveAdminSerialAsync(int id, CancellationToken ct = default)
    {
        var (data, error) = await SendAsync<MessageResponse>(HttpMethod.Delete, $"api/admin/serials/{id}", null, true, ct);
        return (data?.Message, error);
    }

    public Task<(List<LicenseDto>? Data, string? Error)> GetLicensesAsync(string? search = null, int? clientId = null, CancellationToken ct = default) =>
        SendAsync<List<LicenseDto>>(HttpMethod.Get,
            Query("api/licenses", ("search", search), ("clientId", clientId?.ToString())), null, true, ct);

    public Task<(LicenseDto? Data, string? Error)> CreateLicenseAsync(LicenseCreateRequest request, CancellationToken ct = default) =>
        SendAsync<LicenseDto>(HttpMethod.Post, "api/licenses", request, true, ct);

    public Task<(LicenseDto? Data, string? Error)> UpdateLicenseAsync(int id, LicenseUpdateRequest request, CancellationToken ct = default) =>
        SendAsync<LicenseDto>(HttpMethod.Put, $"api/licenses/{id}", request, true, ct);

    public Task<(LicenseDto? Data, string? Error)> ExtendLicenseAsync(int id, int days, CancellationToken ct = default) =>
        SendAsync<LicenseDto>(HttpMethod.Post, $"api/licenses/{id}/extend", new ExtendLicenseRequest { Days = days }, true, ct);

    public Task<(LicenseDto? Data, string? Error)> RevokeLicenseAsync(int id, CancellationToken ct = default) =>
        SendAsync<LicenseDto>(HttpMethod.Post, $"api/licenses/{id}/revoke", null, true, ct);

    public Task<(List<ActivationDto>? Data, string? Error)> GetActivationsAsync(string? search = null, bool? activeOnly = null, CancellationToken ct = default) =>
        SendAsync<List<ActivationDto>>(HttpMethod.Get,
            Query("api/activations", ("search", search), ("activeOnly", activeOnly?.ToString())), null, true, ct);

    public Task<(ActivationDto? Data, string? Error)> DeactivateAsync(int id, string? reason, CancellationToken ct = default) =>
        SendAsync<ActivationDto>(HttpMethod.Post, $"api/activations/{id}/deactivate",
            new DeactivateRequest { Reason = reason }, true, ct);

    public Task<(List<PaymentDto>? Data, string? Error)> GetPaymentsAsync(string? search = null, CancellationToken ct = default) =>
        SendAsync<List<PaymentDto>>(HttpMethod.Get, Query("api/payments", ("search", search)), null, true, ct);

    public Task<(PaymentDto? Data, string? Error)> CreatePaymentAsync(PaymentUpsertRequest request, CancellationToken ct = default) =>
        SendAsync<PaymentDto>(HttpMethod.Post, "api/payments", request, true, ct);

    public Task<(PaymentDto? Data, string? Error)> UpdatePaymentAsync(int id, PaymentUpsertRequest request, CancellationToken ct = default) =>
        SendAsync<PaymentDto>(HttpMethod.Put, $"api/payments/{id}", request, true, ct);

    public Task<(SerialApplyResponse? Data, string? Error)> ApplySerialAsync(SerialApplyRequest request, CancellationToken ct = default) =>
        SendAsync<SerialApplyResponse>(HttpMethod.Post, "api/serial/apply", request, auth: false, ct);

    public Task<(SerialApplyResponse? Data, string? Error)> GetSerialStatusAsync(string serialNumber, CancellationToken ct = default) =>
        SendAsync<SerialApplyResponse>(HttpMethod.Get,
            Query("api/serial/status", ("serialNumber", serialNumber)), null, auth: false, ct);

    public Task<(List<SerialDto>? Data, string? Error)> GetSerialsAsync(string? search = null, string? status = null, CancellationToken ct = default) =>
        SendAsync<List<SerialDto>>(HttpMethod.Get,
            Query("api/serials", ("search", search), ("status", status)), null, true, ct);

    public Task<(SerialDto? Data, string? Error)> ApproveSerialAsync(int id, SerialApproveRequest request, CancellationToken ct = default) =>
        SendAsync<SerialDto>(HttpMethod.Post, $"api/serials/{id}/approve", request, true, ct);

    public Task<(QuickApproveResponse? Data, string? Error)> QuickApproveSerialAsync(int id, string? notes = null, string? planType = null, CancellationToken ct = default) =>
        SendAsync<QuickApproveResponse>(HttpMethod.Post, $"api/serials/{id}/quick-approve",
            new QuickApproveRequest { ReviewNotes = notes, PlanType = planType }, true, ct);

    public Task<(SerialDto? Data, string? Error)> RejectSerialAsync(int id, string? reason, CancellationToken ct = default) =>
        SendAsync<SerialDto>(HttpMethod.Post, $"api/serials/{id}/reject",
            new SerialRejectRequest { ReviewNotes = reason }, true, ct);

    public Task<(SerialDto? Data, string? Error)> RevokeSerialAsync(int id, string? reason, CancellationToken ct = default) =>
        SendAsync<SerialDto>(HttpMethod.Post, $"api/serials/{id}/revoke",
            new SerialRejectRequest { ReviewNotes = reason }, true, ct);

    public Task<(SerialDto? Data, string? Error)> ConfigureSerialAsync(SerialConfigureRequest request, CancellationToken ct = default) =>
        SendAsync<SerialDto>(HttpMethod.Post, "api/serials/configure", request, true, ct);

    private Uri BuildUri(string path)
    {
        var relative = path.TrimStart('/');
        return new Uri($"{_baseUrl}/{relative}");
    }

    private async Task<(T? Data, string? Error)> SendAsync<T>(
        HttpMethod method,
        string path,
        object? body,
        bool auth,
        CancellationToken ct)
    {
        try
        {
            using var request = new HttpRequestMessage(method, BuildUri(path));
            if (auth)
            {
                if (string.IsNullOrWhiteSpace(_session.Token))
                {
                    return (default, "Not signed in.");
                }

                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _session.Token);
            }

            if (body is not null)
            {
                request.Content = JsonContent.Create(body);
            }

            using var response = await _http.SendAsync(request, ct);
            var text = await response.Content.ReadAsStringAsync(ct);

            if (response.IsSuccessStatusCode)
            {
                if (string.IsNullOrWhiteSpace(text) || typeof(T) == typeof(object))
                {
                    return (default, null);
                }

                var data = JsonSerializer.Deserialize<T>(text, JsonOptions);
                return (data, null);
            }

            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                return (default, auth
                    ? "Unauthorized. Please sign in again."
                    : "Invalid login ID or password.");
            }

            try
            {
                var err = JsonSerializer.Deserialize<ApiError>(text, JsonOptions);
                if (!string.IsNullOrWhiteSpace(err?.Message))
                {
                    return (default, err.Message);
                }
            }
            catch
            {
                // fall through
            }

            return (default, string.IsNullOrWhiteSpace(text)
                ? $"Request failed ({(int)response.StatusCode})."
                : text);
        }
        catch (HttpRequestException)
        {
            return (default, $"Cannot reach License API at {BaseUrl}. Start RentalX.LicenseApi first.");
        }
        catch (TaskCanceledException)
        {
            return (default, "Request timed out.");
        }
        catch (InvalidOperationException ex)
        {
            return (default, $"Request setup failed: {ex.Message}");
        }
        catch (UriFormatException)
        {
            return (default, $"Invalid API URL. Use e.g. {LicenseApiDefaults.BaseUrl}");
        }
        catch (Exception ex)
        {
            return (default, ex.Message);
        }
    }

    private static string Query(string path, params (string Key, string? Value)[] pairs)
    {
        var parts = pairs
            .Where(p => !string.IsNullOrWhiteSpace(p.Value))
            .Select(p => $"{Uri.EscapeDataString(p.Key)}={Uri.EscapeDataString(p.Value!)}")
            .ToList();
        return parts.Count == 0 ? path : $"{path}?{string.Join("&", parts)}";
    }
}
