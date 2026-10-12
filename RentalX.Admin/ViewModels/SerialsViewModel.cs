using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RentalX.Admin.Models;
using RentalX.Admin.Services;

namespace RentalX.Admin.ViewModels;

public partial class SerialsViewModel : ObservableObject
{
    private readonly ILicenseApiClient _api;

    [ObservableProperty] private ObservableCollection<SerialDto> _serials = [];
    [ObservableProperty] private ObservableCollection<ClientDto> _clients = [];
    [ObservableProperty] private ObservableCollection<LicenseDto> _clientLicenses = [];
    [ObservableProperty] private SerialDto? _selectedSerial;
    [ObservableProperty] private ClientDto? _approveClient;
    [ObservableProperty] private LicenseDto? _approveLicense;
    [ObservableProperty] private string _searchText = string.Empty;
    [ObservableProperty] private string _statusFilter = "Pending";
    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private string? _errorMessage;
    [ObservableProperty] private string? _successMessage;
    [ObservableProperty] private string _reviewNotes = string.Empty;
    [ObservableProperty] private string _approvePlan = "Yearly";
    [ObservableProperty] private string _selectedSummary = "Select a pending request below.";

    public string[] PlanOptions { get; } = ["Trial", "Monthly", "Yearly", "Lifetime"];
    public string[] StatusFilters { get; } = ["Pending", "Approved", "Deactivated", "Rejected", "Revoked", "All"];

    public SerialsViewModel(ILicenseApiClient api)
    {
        _api = api;
        _ = InitAsync();
    }

    private async Task InitAsync()
    {
        var (clients, _) = await _api.GetClientsAsync();
        if (clients is not null)
        {
            Clients = new ObservableCollection<ClientDto>(clients.Where(c => c.Status == "Active"));
        }

        await LoadAsync();
    }

    partial void OnSearchTextChanged(string value) => _ = LoadAsync();
    partial void OnStatusFilterChanged(string value) => _ = LoadAsync();
    partial void OnApproveClientChanged(ClientDto? value) => _ = LoadApproveLicensesAsync(value?.Id);

    partial void OnSelectedSerialChanged(SerialDto? value)
    {
        ErrorMessage = null;
        if (value is null)
        {
            SelectedSummary = "Select a pending request below.";
            return;
        }

        SelectedSummary =
            $"{value.CompanyName}\n" +
            $"PC: {value.MachineName ?? "—"}\n" +
            $"Serial: {value.DisplaySerial}\n" +
            $"Email: {value.Email}  ·  Phone: {value.Phone}\n" +
            $"Address: {value.Address ?? "—"}  ·  Pin: {value.Pincode ?? "—"}\n" +
            $"Status: {value.Status}";
    }

    private async Task LoadApproveLicensesAsync(int? clientId)
    {
        ClientLicenses.Clear();
        ApproveLicense = null;
        if (clientId is null) return;
        var (data, _) = await _api.GetLicensesAsync(clientId: clientId);
        if (data is not null)
        {
            ClientLicenses = new ObservableCollection<LicenseDto>(data.Where(l => l.Status == "Active"));
            ApproveLicense = ClientLicenses.FirstOrDefault();
        }
    }

    [RelayCommand]
    private async Task LoadAsync()
    {
        IsBusy = true;
        ErrorMessage = null;
        try
        {
            var status = StatusFilter == "All" ? null : StatusFilter;
            var (data, error) = await _api.GetSerialsAsync(SearchText, status);
            if (data is null)
            {
                ErrorMessage = error;
                return;
            }

            Serials = new ObservableCollection<SerialDto>(data);
            if (SelectedSerial is not null)
            {
                SelectedSerial = Serials.FirstOrDefault(s => s.Id == SelectedSerial.Id);
            }
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>Simple path: create everything and approve in one click.</summary>
    [RelayCommand]
    private async Task QuickApproveAsync()
    {
        if (SelectedSerial is null)
        {
            ErrorMessage = "Select a request in the list first.";
            return;
        }

        if (SelectedSerial.Status is not ("Pending" or "Deactivated" or "Revoked"))
        {
            ErrorMessage = "Select a Pending or Deactivated request to approve / re-activate.";
            return;
        }

        var isReactivate = SelectedSerial.Status is "Deactivated" or "Revoked";
        var confirm = MessageBox.Show(
            $"{(isReactivate ? "Re-activate" : "Approve")} {SelectedSerial.CompanyName}?\n\n" +
            $"Plan: {ApprovePlan}\n\n" +
            "This will:\n" +
            "1. Activate their company account\n" +
            "2. Reset Login ID password to RxShop@2026\n" +
            "3. Activate ONE license for the selected plan\n" +
            "4. Allow this computer to login again\n\n" +
            "You will get Username + Password in the next popup — give those to the client.",
            isReactivate ? "Re-activate access" : "Approve request",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (confirm != MessageBoxResult.Yes)
        {
            return;
        }

        IsBusy = true;
        ErrorMessage = null;
        SuccessMessage = null;
        try
        {
            var (data, error) = await _api.QuickApproveSerialAsync(
                SelectedSerial.Id,
                string.IsNullOrWhiteSpace(ReviewNotes) ? null : ReviewNotes.Trim(),
                ApprovePlan);

            if (data is null)
            {
                ErrorMessage = error;
                return;
            }

            SuccessMessage = data.Message;
            MessageBox.Show(
                data.Message,
                "Approved — give these to client",
                MessageBoxButton.OK,
                MessageBoxImage.Information);

            Clipboard.SetText(
                $"Login ID: {data.LoginId}\nPassword: {data.TemporaryPassword}\nLicense: {data.LicenseKey}");

            ReviewNotes = string.Empty;
            var (clients, _) = await _api.GetClientsAsync();
            if (clients is not null)
            {
                Clients = new ObservableCollection<ClientDto>(clients.Where(c => c.Status == "Active"));
            }

            StatusFilter = "Approved";
            await LoadAsync();
            SelectedSerial = Serials.FirstOrDefault(s => s.Id == data.Serial.Id);
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task ApproveAsync()
    {
        if (SelectedSerial is null)
        {
            ErrorMessage = "Select a request first.";
            return;
        }

        if (ApproveClient is null)
        {
            ErrorMessage = "Select an existing client, or use Approve request (recommended).";
            return;
        }

        IsBusy = true;
        ErrorMessage = null;
        try
        {
            var (data, error) = await _api.ApproveSerialAsync(SelectedSerial.Id, new SerialApproveRequest
            {
                ClientId = ApproveClient.Id,
                LicenseId = ApproveLicense?.Id,
                ReviewNotes = string.IsNullOrWhiteSpace(ReviewNotes) ? null : ReviewNotes.Trim()
            });
            if (data is null)
            {
                ErrorMessage = error;
                return;
            }

            SuccessMessage = $"Linked {data.DisplaySerial} to {data.ClientName}. They can login now.";
            ReviewNotes = string.Empty;
            await LoadAsync();
            SelectedSerial = Serials.FirstOrDefault(s => s.Id == data.Id);
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task RejectAsync()
    {
        if (SelectedSerial is null)
        {
            ErrorMessage = "Select a request first.";
            return;
        }

        if (MessageBox.Show("Reject this request? They will not be able to login.", "Reject",
                MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes)
        {
            return;
        }

        IsBusy = true;
        try
        {
            var (data, error) = await _api.RejectSerialAsync(SelectedSerial.Id, ReviewNotes);
            if (data is null)
            {
                ErrorMessage = error;
                return;
            }

            SuccessMessage = "Request rejected.";
            await LoadAsync();
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task RevokeAsync()
    {
        if (SelectedSerial is null)
        {
            ErrorMessage = "Select a serial first.";
            return;
        }

        if (MessageBox.Show(
                "Deactivate this access?\n\nStatus becomes Deactivated.\nClient cannot login (even if was Approved).",
                "Deactivate", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes)
        {
            return;
        }

        IsBusy = true;
        try
        {
            var (data, error) = await _api.RevokeSerialAsync(SelectedSerial.Id, ReviewNotes);
            if (data is null)
            {
                ErrorMessage = error;
                return;
            }

            SuccessMessage = "Deactivated — client cannot login.";
            StatusFilter = "Deactivated";
            await LoadAsync();
            SelectedSerial = Serials.FirstOrDefault(s => s.Id == data.Id);
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void CopySerial()
    {
        if (SelectedSerial is null) return;
        Clipboard.SetText(SelectedSerial.DisplaySerial);
        SuccessMessage = "Display serial copied.";
    }
}
