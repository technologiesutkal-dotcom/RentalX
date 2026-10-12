using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RentalX.Admin.Models;
using RentalX.Admin.Services;

namespace RentalX.Admin.ViewModels;

public partial class PaymentsViewModel : ObservableObject
{
    private readonly ILicenseApiClient _api;

    [ObservableProperty] private ObservableCollection<PaymentDto> _payments = [];
    [ObservableProperty] private ObservableCollection<ClientDto> _clients = [];
    [ObservableProperty] private ObservableCollection<LicenseDto> _clientLicenses = [];
    [ObservableProperty] private PaymentDto? _selectedPayment;
    [ObservableProperty] private ClientDto? _formClient;
    [ObservableProperty] private LicenseDto? _formLicense;
    [ObservableProperty] private string _searchText = string.Empty;
    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private bool _isEditing;
    [ObservableProperty] private string? _errorMessage;
    [ObservableProperty] private string? _successMessage;
    [ObservableProperty] private int? _editingId;
    [ObservableProperty] private decimal _amount;
    [ObservableProperty] private string _currency = "INR";
    [ObservableProperty] private string _method = "UPI";
    [ObservableProperty] private string _reference = string.Empty;
    [ObservableProperty] private string _status = "Completed";
    [ObservableProperty] private DateTime? _paidAt = DateTime.Today;
    [ObservableProperty] private string _notes = string.Empty;

    public string[] MethodOptions { get; } = ["Manual", "UPI", "BankTransfer", "Razorpay", "Cash"];
    public string[] StatusOptions { get; } = ["Pending", "Completed", "Failed", "Refunded"];

    public PaymentsViewModel(ILicenseApiClient api)
    {
        _api = api;
        _ = InitAsync();
    }

    private async Task InitAsync()
    {
        var (clients, _) = await _api.GetClientsAsync();
        if (clients is not null)
        {
            Clients = new ObservableCollection<ClientDto>(clients);
        }

        await LoadAsync();
    }

    partial void OnSearchTextChanged(string value) => _ = LoadAsync();

    partial void OnFormClientChanged(ClientDto? value)
    {
        _ = LoadClientLicensesAsync(value?.Id);
    }

    private async Task LoadClientLicensesAsync(int? clientId)
    {
        ClientLicenses.Clear();
        FormLicense = null;
        if (clientId is null)
        {
            return;
        }

        var (data, _) = await _api.GetLicensesAsync(clientId: clientId);
        if (data is not null)
        {
            ClientLicenses = new ObservableCollection<LicenseDto>(data);
        }
    }

    [RelayCommand]
    private async Task LoadAsync()
    {
        IsBusy = true;
        ErrorMessage = null;
        try
        {
            var (data, error) = await _api.GetPaymentsAsync(SearchText);
            if (data is null)
            {
                ErrorMessage = error;
                return;
            }

            Payments = new ObservableCollection<PaymentDto>(data);
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void NewPayment()
    {
        EditingId = null;
        IsEditing = true;
        ErrorMessage = null;
        SuccessMessage = null;
        FormClient = Clients.FirstOrDefault();
        Amount = 0;
        Currency = "INR";
        Method = "UPI";
        Reference = string.Empty;
        Status = "Completed";
        PaidAt = DateTime.Today;
        Notes = string.Empty;
    }

    [RelayCommand]
    private async Task EditSelected()
    {
        if (SelectedPayment is null)
        {
            ErrorMessage = "Select a payment first.";
            return;
        }

        EditingId = SelectedPayment.Id;
        IsEditing = true;
        ErrorMessage = null;
        SuccessMessage = null;
        FormClient = Clients.FirstOrDefault(c => c.Id == SelectedPayment.ClientId);
        await LoadClientLicensesAsync(SelectedPayment.ClientId);
        FormLicense = ClientLicenses.FirstOrDefault(l => l.Id == SelectedPayment.LicenseId);
        Amount = SelectedPayment.Amount;
        Currency = SelectedPayment.Currency;
        Method = SelectedPayment.Method;
        Reference = SelectedPayment.Reference ?? string.Empty;
        Status = SelectedPayment.Status;
        PaidAt = SelectedPayment.PaidAt.ToLocalTime().Date;
        Notes = SelectedPayment.Notes ?? string.Empty;
    }

    [RelayCommand]
    private void CancelEdit()
    {
        IsEditing = false;
        ErrorMessage = null;
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        ErrorMessage = null;
        SuccessMessage = null;

        if (FormClient is null)
        {
            ErrorMessage = "Select a client.";
            return;
        }

        if (Amount <= 0)
        {
            ErrorMessage = "Amount must be greater than zero.";
            return;
        }

        var request = new PaymentUpsertRequest
        {
            ClientId = FormClient.Id,
            LicenseId = FormLicense?.Id,
            Amount = Amount,
            Currency = Currency.Trim().ToUpperInvariant(),
            Method = Method,
            Reference = string.IsNullOrWhiteSpace(Reference) ? null : Reference.Trim(),
            Status = Status,
            PaidAt = PaidAt,
            Notes = string.IsNullOrWhiteSpace(Notes) ? null : Notes.Trim()
        };

        IsBusy = true;
        try
        {
            var (data, error) = EditingId is int id
                ? await _api.UpdatePaymentAsync(id, request)
                : await _api.CreatePaymentAsync(request);

            if (data is null)
            {
                ErrorMessage = error ?? "Save failed.";
                return;
            }

            SuccessMessage = EditingId is null ? "Payment recorded." : "Payment updated.";
            IsEditing = false;
            EditingId = null;
            await LoadAsync();
            SelectedPayment = Payments.FirstOrDefault(p => p.Id == data.Id);
        }
        finally
        {
            IsBusy = false;
        }
    }
}
