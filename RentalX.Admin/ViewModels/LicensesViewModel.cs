using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RentalX.Admin.Models;
using RentalX.Admin.Services;

namespace RentalX.Admin.ViewModels;

public partial class LicensesViewModel : ObservableObject
{
    private readonly ILicenseApiClient _api;

    [ObservableProperty] private ObservableCollection<LicenseDto> _licenses = [];
    [ObservableProperty] private ObservableCollection<ClientDto> _clients = [];
    [ObservableProperty] private LicenseDto? _selectedLicense;
    [ObservableProperty] private ClientDto? _selectedClient;
    [ObservableProperty] private string _searchText = string.Empty;
    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private bool _isCreating;
    [ObservableProperty] private string? _errorMessage;
    [ObservableProperty] private string? _successMessage;
    [ObservableProperty] private string _planType = "Monthly";
    [ObservableProperty] private int _maxMachines = 1;
    [ObservableProperty] private DateTime? _startsAt = DateTime.Today;
    [ObservableProperty] private DateTime? _expiresAt = DateTime.Today.AddMonths(1);
    [ObservableProperty] private string _notes = string.Empty;
    [ObservableProperty] private int _extendDays = 30;

    public string[] PlanOptions { get; } = ["Trial", "Monthly", "Yearly", "Lifetime"];

    public LicensesViewModel(ILicenseApiClient api)
    {
        _api = api;
        _ = InitAsync();
    }

    private async Task InitAsync()
    {
        await LoadClientsAsync();
        await LoadAsync();
    }

    partial void OnSearchTextChanged(string value) => _ = LoadAsync();

    [RelayCommand]
    private async Task LoadAsync()
    {
        IsBusy = true;
        ErrorMessage = null;
        try
        {
            var (data, error) = await _api.GetLicensesAsync(SearchText);
            if (data is null)
            {
                ErrorMessage = error;
                return;
            }

            Licenses = new ObservableCollection<LicenseDto>(data);
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task LoadClientsAsync()
    {
        var (data, _) = await _api.GetClientsAsync();
        if (data is not null)
        {
            Clients = new ObservableCollection<ClientDto>(data.Where(c => c.Status == "Active"));
        }
    }

    [RelayCommand]
    private void StartCreate()
    {
        IsCreating = true;
        ErrorMessage = null;
        SuccessMessage = null;
        PlanType = !string.IsNullOrWhiteSpace(SelectedLicense?.PlanType) ? SelectedLicense!.PlanType : "Monthly";
        MaxMachines = SelectedLicense is { MaxMachines: > 0 } ? SelectedLicense.MaxMachines : 1;
        StartsAt = DateTime.Today;
        ExpiresAt = DateTime.Today.AddMonths(1);
        Notes = string.Empty;
        SelectedClient = SelectedLicense is not null
            ? Clients.FirstOrDefault(c => c.Id == SelectedLicense.ClientId) ?? Clients.FirstOrDefault()
            : Clients.FirstOrDefault();
        OnPlanTypeChanged(PlanType);
    }

    [RelayCommand]
    private void CancelCreate()
    {
        IsCreating = false;
        ErrorMessage = null;
    }

    [RelayCommand]
    private async Task CreateAsync()
    {
        ErrorMessage = null;
        SuccessMessage = null;

        if (SelectedClient is null)
        {
            ErrorMessage = "Select a client.";
            return;
        }

        if (MaxMachines is < 1 or > 50)
        {
            ErrorMessage = "Max machines must be between 1 and 50.";
            return;
        }

        if (PlanType != "Lifetime" && ExpiresAt is DateTime exp && StartsAt is DateTime start && exp.Date < start.Date)
        {
            ErrorMessage = "Expiry must be on or after start date.";
            return;
        }

        IsBusy = true;
        try
        {
            var (data, error) = await _api.CreateLicenseAsync(new LicenseCreateRequest
            {
                ClientId = SelectedClient.Id,
                PlanType = PlanType,
                MaxMachines = MaxMachines,
                StartsAt = StartsAt,
                ExpiresAt = PlanType == "Lifetime" ? null : ExpiresAt,
                Notes = string.IsNullOrWhiteSpace(Notes) ? null : Notes.Trim()
            });

            if (data is null)
            {
                ErrorMessage = error ?? "Could not create license.";
                return;
            }

            SuccessMessage = $"Plan updated for {SelectedClient.CompanyName}: {data.PlanType} — {data.LicenseKey}";
            IsCreating = false;
            await LoadAsync();
            SelectedLicense = Licenses.FirstOrDefault(l => l.Id == data.Id);
            Clipboard.SetText(data.LicenseKey);
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void CopyKey()
    {
        if (SelectedLicense is null)
        {
            ErrorMessage = "Select a license first.";
            return;
        }

        Clipboard.SetText(SelectedLicense.LicenseKey);
        SuccessMessage = "License key copied.";
    }

    [RelayCommand]
    private async Task ExtendAsync()
    {
        if (SelectedLicense is null)
        {
            ErrorMessage = "Select a license first.";
            return;
        }

        if (ExtendDays is < 1 or > 3650)
        {
            ErrorMessage = "Extend days must be between 1 and 3650.";
            return;
        }

        IsBusy = true;
        ErrorMessage = null;
        try
        {
            var (data, error) = await _api.ExtendLicenseAsync(SelectedLicense.Id, ExtendDays);
            if (data is null)
            {
                ErrorMessage = error;
                return;
            }

            SuccessMessage = $"Extended to {data.ExpiresAt:dd MMM yyyy}.";
            await LoadAsync();
            SelectedLicense = Licenses.FirstOrDefault(l => l.Id == data.Id);
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task RevokeAsync()
    {
        if (SelectedLicense is null)
        {
            ErrorMessage = "Select a license first.";
            return;
        }

        var confirm = MessageBox.Show(
            $"Revoke license {SelectedLicense.LicenseKey}?\nAll activations will be deactivated.",
            "Revoke License",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);
        if (confirm != MessageBoxResult.Yes)
        {
            return;
        }

        IsBusy = true;
        ErrorMessage = null;
        try
        {
            var (data, error) = await _api.RevokeLicenseAsync(SelectedLicense.Id);
            if (data is null)
            {
                ErrorMessage = error;
                return;
            }

            SuccessMessage = "License revoked.";
            await LoadAsync();
            SelectedLicense = Licenses.FirstOrDefault(l => l.Id == data.Id);
        }
        finally
        {
            IsBusy = false;
        }
    }

    partial void OnPlanTypeChanged(string value)
    {
        if (StartsAt is not DateTime start)
        {
            return;
        }

        ExpiresAt = value switch
        {
            "Trial" => start.AddDays(14),
            "Monthly" => start.AddMonths(1),
            "Yearly" => start.AddYears(1),
            "Lifetime" => start.AddYears(100),
            _ => ExpiresAt
        };
    }
}
