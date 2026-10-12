using System.IO;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using RentalX.Admin.Services;
using RentalX.Models.Licensing;

namespace RentalX.Services.Licensing;

public sealed class LicenseClientService : ILicenseClientService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true
    };

    private readonly HttpClient _http;
    private readonly IMachineIdService _machineId;
    private readonly string _statePath;
    private string _baseUrl = LicenseApiDefaults.BaseUrl;

    public LicenseClientService(HttpClient http, IMachineIdService machineId)
    {
        _http = http;
        _machineId = machineId;
        var folder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "RentalX");
        Directory.CreateDirectory(folder);
        _statePath = Path.Combine(folder, "license.json");
        Current = Load();
    }

    public string ApiBaseUrl
    {
        get => _baseUrl;
        set
        {
            var trimmed = (value ?? string.Empty).Trim().TrimEnd('/');
            _baseUrl = string.IsNullOrWhiteSpace(trimmed) ? LicenseApiDefaults.BaseUrl : trimmed;
        }
    }

    public LicenseLocalState? Current { get; private set; }
    public bool HasLocalLicense =>
        Current is not null && !string.IsNullOrWhiteSpace(Current.LicenseKey);

    public async Task<(bool Ok, string Message)> ActivateAsync(string licenseKey, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(licenseKey) || licenseKey.Trim().Length < 8)
        {
            return (false, "Enter a valid license key.");
        }

        try
        {
            var payload = new
            {
                licenseKey = licenseKey.Trim(),
                machineId = _machineId.GetMachineId(),
                machineName = _machineId.GetMachineName(),
                osInfo = $"{Environment.OSVersion}"
            };

            using var response = await _http.PostAsJsonAsync(BuildUri("api/license/activate"), payload, ct);
            var text = await response.Content.ReadAsStringAsync(ct);
            var result = JsonSerializer.Deserialize<LicenseStatusDto>(text, JsonOptions);

            if (!response.IsSuccessStatusCode || result is null || !result.IsValid)
            {
                return (false, result?.Message ?? "Activation failed.");
            }

            Current = new LicenseLocalState
            {
                LicenseKey = licenseKey.Trim().ToUpperInvariant(),
                MachineId = _machineId.GetMachineId(),
                CompanyName = result.CompanyName,
                PlanType = result.PlanType,
                ExpiresAt = result.ExpiresAt,
                ActivatedAt = DateTime.UtcNow,
                LastValidatedAt = DateTime.UtcNow
            };
            Save(Current);
            return (true, result.Message);
        }
        catch (HttpRequestException)
        {
            return (false, $"Cannot reach license server at {ApiBaseUrl}.");
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }

    public async Task<(bool Ok, string Message)> ValidateAsync(CancellationToken ct = default)
    {
        if (Current is null)
        {
            return (false, "No license activated on this machine.");
        }

        try
        {
            var payload = new
            {
                licenseKey = Current.LicenseKey,
                machineId = _machineId.GetMachineId()
            };

            using var response = await _http.PostAsJsonAsync(BuildUri("api/license/validate"), payload, ct);
            var text = await response.Content.ReadAsStringAsync(ct);
            var result = JsonSerializer.Deserialize<LicenseStatusDto>(text, JsonOptions);

            if (!response.IsSuccessStatusCode || result is null || !result.IsValid)
            {
                return (false, result?.Message ?? "License validation failed.");
            }

            Current.CompanyName = result.CompanyName ?? Current.CompanyName;
            Current.PlanType = result.PlanType ?? Current.PlanType;
            Current.ExpiresAt = result.ExpiresAt ?? Current.ExpiresAt;
            Current.LastValidatedAt = DateTime.UtcNow;
            Save(Current);
            return (true, result.Message);
        }
        catch (HttpRequestException)
        {
            if (Current.ExpiresAt is DateTime exp && exp.Date >= DateTime.UtcNow.Date)
            {
                return (true, "Offline mode: using last known valid license.");
            }

            return (false, $"Cannot reach license server at {ApiBaseUrl}.");
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }

    public void ClearLocal()
    {
        Current = null;
        if (File.Exists(_statePath))
        {
            File.Delete(_statePath);
        }
    }

    private Uri BuildUri(string path) => new($"{_baseUrl}/{path.TrimStart('/')}");

    private LicenseLocalState? Load()
    {
        if (!File.Exists(_statePath))
        {
            return null;
        }

        try
        {
            var json = File.ReadAllText(_statePath);
            return JsonSerializer.Deserialize<LicenseLocalState>(json, JsonOptions);
        }
        catch
        {
            return null;
        }
    }

    private void Save(LicenseLocalState state) =>
        File.WriteAllText(_statePath, JsonSerializer.Serialize(state, JsonOptions));

    private sealed class LicenseStatusDto
    {
        public bool IsValid { get; set; }
        public string Message { get; set; } = string.Empty;
        public string? CompanyName { get; set; }
        public string? PlanType { get; set; }
        public DateTime? ExpiresAt { get; set; }
    }
}
