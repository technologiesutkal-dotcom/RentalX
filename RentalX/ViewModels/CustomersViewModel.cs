using System.Collections.ObjectModel;
using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RentalX.Data.Repositories;
using RentalX.Helpers;
using RentalX.Models;
using RentalX.ViewModels.Base;

namespace RentalX.ViewModels;

public partial class CustomersViewModel : PageViewModelBase
{
    private readonly ICustomerRepository _customers;
    private List<Customer> _all = [];

    public CustomersViewModel(ICustomerRepository customers)
    {
        _customers = customers;
    }

    public ObservableCollection<Customer> Customers { get; } = new();

    [ObservableProperty] private Customer? _selectedCustomer;
    [ObservableProperty] private string _nameText = string.Empty;
    [ObservableProperty] private string _phoneText = string.Empty;
    [ObservableProperty] private string _emailText = string.Empty;
    [ObservableProperty] private string _addressText = string.Empty;
    [ObservableProperty] private string _searchText = string.Empty;
    [ObservableProperty] private string _resultCountText = "0 customers";
    [ObservableProperty] private string _formMessage = string.Empty;
    [ObservableProperty] private Brush _formMessageBrush = Brushes.Transparent;
    [ObservableProperty] private string _nameError = string.Empty;
    [ObservableProperty] private string _phoneError = string.Empty;
    [ObservableProperty] private string _emailError = string.Empty;

    partial void OnSearchTextChanged(string value) => ApplyFilter();

    public override async Task LoadAsync()
    {
        IsBusy = true;
        var selectedId = SelectedCustomer?.Id;
        _all = await _customers.GetAllAsync();
        ApplyFilter();
        SelectedCustomer = selectedId is null
            ? null
            : Customers.FirstOrDefault(x => x.Id == selectedId);
        IsBusy = false;
    }

    private void ApplyFilter()
    {
        var filtered = _all.Where(c =>
            ListFilterHelper.Matches(SearchText, c.Name, c.Phone, c.Email, c.Address));
        ListFilterHelper.Replace(Customers, filtered);
        ResultCountText = $"{Customers.Count} customer(s)";
    }

    [RelayCommand]
    private void ClearSearch() => SearchText = string.Empty;

    partial void OnSelectedCustomerChanged(Customer? value)
    {
        ClearErrors();
        if (value is null)
        {
            return;
        }

        NameText = value.Name;
        PhoneText = value.Phone;
        EmailText = value.Email;
        AddressText = value.Address;
    }

    [RelayCommand]
    private void ClearForm()
    {
        SelectedCustomer = null;
        NameText = PhoneText = EmailText = AddressText = string.Empty;
        ClearErrors();
        FormMessage = string.Empty;
    }

    [RelayCommand]
    private async Task SaveCustomerAsync()
    {
        ClearErrors();
        var valid = true;

        if (!ValidationHelper.IsValidName(NameText))
        {
            NameError = "Enter a valid name (min 3 letters).";
            valid = false;
        }

        if (!ValidationHelper.IsValidPhone(PhoneText))
        {
            PhoneError = "Enter a valid phone (10–15 digits).";
            valid = false;
        }

        if (!ValidationHelper.IsValidOptionalEmail(EmailText))
        {
            EmailError = "Enter a valid email or leave blank.";
            valid = false;
        }

        if (!valid)
        {
            SetError("Please fix customer form errors.");
            return;
        }

        try
        {
            if (SelectedCustomer is null)
            {
                await _customers.AddAsync(new Customer
                {
                    Name = NameText.Trim(),
                    Phone = PhoneText.Trim(),
                    Email = EmailText.Trim(),
                    Address = AddressText.Trim()
                });
                SetSuccess($"Customer {NameText.Trim()} added.");
            }
            else
            {
                var existing = await _customers.GetByIdAsync(SelectedCustomer.Id)
                    ?? throw new InvalidOperationException("Customer not found.");
                existing.Name = NameText.Trim();
                existing.Phone = PhoneText.Trim();
                existing.Email = EmailText.Trim();
                existing.Address = AddressText.Trim();
                await _customers.UpdateAsync(existing);
                SetSuccess($"Customer {existing.Name} updated.");
            }

            NameText = PhoneText = EmailText = AddressText = string.Empty;
            SelectedCustomer = null;
            await LoadAsync();
        }
        catch (Exception ex)
        {
            SetError(ex.Message);
        }
    }

    [RelayCommand]
    private async Task DeleteCustomerAsync()
    {
        if (SelectedCustomer is null)
        {
            SetError("Select a customer to delete.");
            return;
        }

        try
        {
            var name = SelectedCustomer.Name;
            await _customers.DeleteAsync(SelectedCustomer.Id);
            ClearForm();
            SetSuccess($"Customer {name} deleted.");
            await LoadAsync();
        }
        catch (Exception ex)
        {
            SetError(ex.Message);
        }
    }

    private void ClearErrors()
    {
        NameError = PhoneError = EmailError = string.Empty;
    }

    private void SetError(string message)
    {
        FormMessage = message;
        FormMessageBrush = new SolidColorBrush(Color.FromRgb(220, 38, 38));
    }

    private void SetSuccess(string message)
    {
        FormMessage = message;
        FormMessageBrush = new SolidColorBrush(Color.FromRgb(5, 150, 105));
    }
}
