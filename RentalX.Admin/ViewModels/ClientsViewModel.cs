using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RentalX.Admin.Models;
using RentalX.Admin.Services;

namespace RentalX.Admin.ViewModels;

public partial class ClientsViewModel : ObservableObject
{
    private readonly ILicenseApiClient _api;

    [ObservableProperty] private ObservableCollection<ClientDto> _clients = [];
    [ObservableProperty] private ClientDto? _selectedClient;
    [ObservableProperty] private string _searchText = string.Empty;
    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private bool _isEditing;
    [ObservableProperty] private string? _errorMessage;
    [ObservableProperty] private string? _successMessage;
    [ObservableProperty] private int? _editingId;

    [ObservableProperty] private string _companyName = string.Empty;
    [ObservableProperty] private string _contactName = string.Empty;
    [ObservableProperty] private string _email = string.Empty;
    [ObservableProperty] private string _phone = string.Empty;
    [ObservableProperty] private string _city = string.Empty;
    [ObservableProperty] private string _address = string.Empty;
    [ObservableProperty] private string _notes = string.Empty;
    [ObservableProperty] private string _status = "Active";
    [ObservableProperty] private string _portalUsername = "—";
    [ObservableProperty] private string _portalPassword = "—";

    public string[] StatusOptions { get; } = ["Active", "Suspended", "Inactive", "Deactivated"];

    public ClientsViewModel(ILicenseApiClient api)
    {
        _api = api;
        _ = LoadAsync();
    }

    partial void OnSearchTextChanged(string value) => _ = LoadAsync();

    partial void OnSelectedClientChanged(ClientDto? value)
    {
        if (value is null || IsEditing)
        {
            return;
        }

        PopulateForm(value);
    }

    [RelayCommand]
    private async Task LoadAsync()
    {
        IsBusy = true;
        ErrorMessage = null;
        try
        {
            var (data, error) = await _api.GetClientsAsync(SearchText);
            if (data is null)
            {
                ErrorMessage = error;
                return;
            }

            Clients = new ObservableCollection<ClientDto>(data);
            if (SelectedClient is not null)
            {
                SelectedClient = Clients.FirstOrDefault(c => c.Id == SelectedClient.Id);
            }
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void NewClient()
    {
        ErrorMessage = "Do not create clients here. Open Requests → Approve request.";
        SuccessMessage = null;
        IsEditing = false;
    }

    [RelayCommand]
    private void EditSelected()
    {
        if (SelectedClient is null)
        {
            ErrorMessage = "Select a client first.";
            return;
        }

        EditingId = SelectedClient.Id;
        PopulateForm(SelectedClient);
        IsEditing = true;
        SuccessMessage = null;
        ErrorMessage = null;
    }

    [RelayCommand]
    private void CancelEdit()
    {
        IsEditing = false;
        ErrorMessage = null;
        if (SelectedClient is not null)
        {
            PopulateForm(SelectedClient);
        }
    }

    [RelayCommand]
    private void CopyCredentials()
    {
        if (SelectedClient is null)
        {
            ErrorMessage = "Select a client first.";
            return;
        }

        var id = SelectedClient.PortalUsername ?? "—";
        var pwd = string.IsNullOrWhiteSpace(SelectedClient.PortalPassword) ? "RxShop@2026" : SelectedClient.PortalPassword;
        Clipboard.SetText($"Login ID: {id}\nPassword: {pwd}");
        SuccessMessage = "Login ID and password copied.";
    }

    [RelayCommand]
    private async Task DeactivateAsync()
    {
        if (SelectedClient is null)
        {
            ErrorMessage = "Select a client first.";
            return;
        }

        if (MessageBox.Show(
                $"Deactivate {SelectedClient.CompanyName}?\n\n" +
                "Status → Deactivated. Client cannot login.",
                "Deactivate client",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning) != MessageBoxResult.Yes)
        {
            return;
        }

        IsBusy = true;
        ErrorMessage = null;
        try
        {
            var (data, error) = await _api.DeactivateClientAsync(SelectedClient.Id);
            if (data is null)
            {
                ErrorMessage = error;
                return;
            }

            SuccessMessage = $"{data.CompanyName} is Deactivated — login blocked.";
            await LoadAsync();
            SelectedClient = Clients.FirstOrDefault(c => c.Id == data.Id);
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task ReactivateAsync()
    {
        if (SelectedClient is null)
        {
            ErrorMessage = "Select a client first.";
            return;
        }

        if (MessageBox.Show(
                $"Re-activate {SelectedClient.CompanyName}?\n\n" +
                "• Status → Active\n" +
                "• License restored\n" +
                "• PC serials Approved again\n" +
                "• Password reset to RxShop@2026\n\n" +
                "Client can login again after this.",
                "Re-activate client",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question) != MessageBoxResult.Yes)
        {
            return;
        }

        IsBusy = true;
        ErrorMessage = null;
        try
        {
            var (data, error) = await _api.ReactivateClientAsync(SelectedClient.Id);
            if (data is null)
            {
                ErrorMessage = error;
                return;
            }

            await LoadAsync();
            SelectedClient = Clients.FirstOrDefault(c => c.Id == data.Id);
            var id = data.PortalUsername ?? "—";
            var pwd = string.IsNullOrWhiteSpace(data.PortalPassword) ? "RxShop@2026" : data.PortalPassword;
            SuccessMessage = $"Re-activated. Login ID: {id}  Password: {pwd}";
            MessageBox.Show(
                $"Client can login now.\n\nLogin ID: {id}\nPassword: {pwd}",
                "Re-activated",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            Clipboard.SetText($"Login ID: {id}\nPassword: {pwd}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        ErrorMessage = null;
        SuccessMessage = null;

        if (string.IsNullOrWhiteSpace(CompanyName) || CompanyName.Trim().Length < 2)
        {
            ErrorMessage = "Company name is required (min 2 characters).";
            return;
        }

        if (string.IsNullOrWhiteSpace(ContactName) || ContactName.Trim().Length < 2)
        {
            ErrorMessage = "Contact name is required.";
            return;
        }

        if (string.IsNullOrWhiteSpace(Email) || !Email.Contains('@'))
        {
            ErrorMessage = "Enter a valid email address.";
            return;
        }

        if (string.IsNullOrWhiteSpace(Phone) || Phone.Trim().Length < 8)
        {
            ErrorMessage = "Enter a valid phone number (min 8 digits).";
            return;
        }

        var request = new ClientUpsertRequest
        {
            CompanyName = CompanyName.Trim(),
            ContactName = ContactName.Trim(),
            Email = Email.Trim(),
            Phone = Phone.Trim(),
            City = string.IsNullOrWhiteSpace(City) ? null : City.Trim(),
            Address = string.IsNullOrWhiteSpace(Address) ? null : Address.Trim(),
            Notes = string.IsNullOrWhiteSpace(Notes) ? null : Notes.Trim(),
            Status = Status
        };

        IsBusy = true;
        try
        {
            if (EditingId is not int id)
            {
                ErrorMessage = "Do not create clients here. Open Requests → Approve request.";
                return;
            }

            // Changing status to Active from Deactivated → full reactivate
            if (SelectedClient?.Status == "Deactivated" && Status == "Active")
            {
                var (reactivated, reactivateError) = await _api.ReactivateClientAsync(id);
                if (reactivated is null)
                {
                    ErrorMessage = reactivateError;
                    return;
                }

                SuccessMessage = $"Re-activated. Login: {reactivated.PortalUsername} / {reactivated.PortalPassword}";
                IsEditing = false;
                EditingId = null;
                await LoadAsync();
                SelectedClient = Clients.FirstOrDefault(c => c.Id == reactivated.Id);
                return;
            }

            var (data, error) = await _api.UpdateClientAsync(id, request);
            if (data is null)
            {
                ErrorMessage = error ?? "Save failed.";
                return;
            }

            SuccessMessage = "Client updated.";
            IsEditing = false;
            EditingId = null;
            await LoadAsync();
            SelectedClient = Clients.FirstOrDefault(c => c.Id == data.Id);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void PopulateForm(ClientDto c)
    {
        CompanyName = c.CompanyName;
        ContactName = c.ContactName;
        Email = c.Email;
        Phone = c.Phone;
        City = c.City ?? string.Empty;
        Address = c.Address ?? string.Empty;
        Notes = c.Notes ?? string.Empty;
        Status = c.Status;
        PortalUsername = string.IsNullOrWhiteSpace(c.PortalUsername) ? "—" : c.PortalUsername;
        PortalPassword = string.IsNullOrWhiteSpace(c.PortalPassword) ? "RxShop@2026" : c.PortalPassword;
    }
}
