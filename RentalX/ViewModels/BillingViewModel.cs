using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RentalX.Data.Repositories;
using RentalX.Helpers;
using RentalX.Models;
using RentalX.Services;
using RentalX.ViewModels.Base;

namespace RentalX.ViewModels;

public partial class BillingViewModel : PageViewModelBase
{
    private readonly IBookingRepository _bookings;
    private readonly IRentalOperationsService _operations;
    private List<Booking> _all = [];

    public BillingViewModel(IBookingRepository bookings, IRentalOperationsService operations)
    {
        _bookings = bookings;
        _operations = operations;
    }

    public ObservableCollection<Booking> Invoices { get; } = new();

    [ObservableProperty] private Booking? _selectedBooking;
    [ObservableProperty] private string _searchText = string.Empty;
    [ObservableProperty] private string _resultCountText = "0 records";
    [ObservableProperty] private string _formMessage = string.Empty;
    [ObservableProperty] private Brush _formMessageBrush = Brushes.Transparent;

    partial void OnSearchTextChanged(string value) => ApplyFilter();

    public override async Task LoadAsync()
    {
        IsBusy = true;
        _all = await _bookings.GetAllAsync();
        ApplyFilter();
        IsBusy = false;
    }

    private void ApplyFilter()
    {
        var filtered = _all.Where(b =>
            ListFilterHelper.Matches(
                SearchText,
                b.BookingNumber,
                b.InvoiceText,
                b.CustomerName,
                b.ItemsSummary,
                b.AmountText,
                b.StatusText,
                b.PaymentStatusText));
        ListFilterHelper.Replace(Invoices, filtered);
        ResultCountText = $"{Invoices.Count} record(s)";
    }

    [RelayCommand]
    private void ClearSearch() => SearchText = string.Empty;

    [RelayCommand]
    private async Task GenerateInvoiceAsync()
    {
        if (SelectedBooking is null)
        {
            SetError("Select a booking to generate invoice.");
            return;
        }

        try
        {
            var path = await _operations.GenerateInvoicePdfAsync(SelectedBooking.Id);
            SetSuccess($"Invoice generated for {SelectedBooking.BookingNumber}.");
            MessageBox.Show("Invoice PDF generated successfully.", "Invoice", MessageBoxButton.OK, MessageBoxImage.Information);
            await LoadAsync();
        }
        catch (Exception ex)
        {
            SetError(ex.Message);
        }
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
