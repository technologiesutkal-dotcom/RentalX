using System.Collections.ObjectModel;
using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RentalX.Data.Repositories;
using RentalX.Helpers;
using RentalX.Models;
using RentalX.Services;
using RentalX.Services.Licensing;
using RentalX.ViewModels.Base;

namespace RentalX.ViewModels;

public partial class BookingsViewModel : PageViewModelBase
{
    private readonly IBookingRepository _bookings;
    private readonly ICustomerRepository _customers;
    private readonly IInventoryRepository _inventory;
    private readonly IRentalOperationsService _operations;
    private readonly IClientAccessMonitor _access;
    private bool _syncingDaysFromDates;
    private List<Booking> _allBookings = [];

    public BookingsViewModel(
        IBookingRepository bookings,
        ICustomerRepository customers,
        IInventoryRepository inventory,
        IRentalOperationsService operations,
        IClientAccessMonitor access)
    {
        _bookings = bookings;
        _customers = customers;
        _inventory = inventory;
        _operations = operations;
        _access = access;
        BookingDate = DateTime.Today;
        ReturnDate = DateTime.Today.AddDays(1);
        AdvanceText = "0";
    }

    public ObservableCollection<Booking> Bookings { get; } = new();
    public ObservableCollection<Customer> Customers { get; } = new();
    public ObservableCollection<InventoryItem> InventoryItems { get; } = new();
    public ObservableCollection<BookingCartItem> CartItems { get; } = new();

    public string WorkflowHint { get; } =
        "Step 1 of 3 — BOOKING: Add one or more items for the same customer, then Create Booking. " +
        "Stock stays in warehouse until Dispatch → Receive.";

    [ObservableProperty] private Booking? _selectedBooking;
    [ObservableProperty] private Customer? _selectedCustomer;
    [ObservableProperty] private InventoryItem? _selectedInventoryItem;
    [ObservableProperty] private string _quantityText = "1";
    [ObservableProperty] private string _rentalDaysText = "1";
    [ObservableProperty] private string _amountText = string.Empty;
    [ObservableProperty] private string _advanceText = "0";
    [ObservableProperty] private string _balancePreview = string.Empty;
    [ObservableProperty] private string _rateHint = string.Empty;
    [ObservableProperty] private string _cartCountText = "0 items";
    [ObservableProperty] private DateTime? _bookingDate;
    [ObservableProperty] private DateTime? _returnDate;
    [ObservableProperty] private string _formMessage = string.Empty;
    [ObservableProperty] private Brush _formMessageBrush = Brushes.Transparent;
    [ObservableProperty] private string _customerError = string.Empty;
    [ObservableProperty] private string _itemError = string.Empty;
    [ObservableProperty] private string _quantityError = string.Empty;
    [ObservableProperty] private string _daysError = string.Empty;
    [ObservableProperty] private string _dateError = string.Empty;
    [ObservableProperty] private string _advanceError = string.Empty;
    [ObservableProperty] private string _newCustomerName = string.Empty;
    [ObservableProperty] private string _newCustomerPhone = string.Empty;
    [ObservableProperty] private string _searchText = string.Empty;
    [ObservableProperty] private string _resultCountText = "0 records";

    partial void OnSearchTextChanged(string value) => ApplyFilter();
    partial void OnSelectedInventoryItemChanged(InventoryItem? value) => UpdateRateHint();
    partial void OnQuantityTextChanged(string value) => UpdateRateHint();
    partial void OnAdvanceTextChanged(string value) => RecalculateAmount();
    partial void OnRentalDaysTextChanged(string value)
    {
        if (!_syncingDaysFromDates)
        {
            SyncReturnDateFromDays();
        }

        RecalculateAmount();
    }

    partial void OnBookingDateChanged(DateTime? value) => SyncDaysFromDates();
    partial void OnReturnDateChanged(DateTime? value) => SyncDaysFromDates();

    public override async Task LoadAsync()
    {
        IsBusy = true;
        _allBookings = await _bookings.GetAllAsync();
        ApplyFilter();

        Customers.Clear();
        foreach (var customer in await _customers.GetAllAsync())
        {
            Customers.Add(customer);
        }

        InventoryItems.Clear();
        foreach (var item in await _inventory.GetAllAsync())
        {
            if (string.Equals(item.Status, "Active", StringComparison.OrdinalIgnoreCase))
            {
                InventoryItems.Add(item);
            }
        }

        SelectedCustomer ??= Customers.FirstOrDefault();
        SelectedInventoryItem ??= InventoryItems.FirstOrDefault(x => x.AvailableQuantity > 0) ?? InventoryItems.FirstOrDefault();
        RecalculateAmount();
        IsBusy = false;
    }

    private void ApplyFilter()
    {
        var filtered = _allBookings.Where(b =>
            ListFilterHelper.Matches(
                SearchText,
                b.BookingNumber,
                b.CustomerName,
                b.ItemsSummary,
                b.StatusText,
                b.PaymentStatusText,
                b.AmountText,
                b.InvoiceText));
        ListFilterHelper.Replace(Bookings, filtered);
        ResultCountText = $"{Bookings.Count} record(s)";
    }

    [RelayCommand]
    private void ClearSearch() => SearchText = string.Empty;

    [RelayCommand]
    private void AddItemToCart()
    {
        ClearErrors();
        if (SelectedInventoryItem is null)
        {
            ItemError = "Select an inventory item.";
            SetError("Select an item to add.");
            return;
        }

        if (!ValidationHelper.IsValidPositiveInt(QuantityText, out var qty))
        {
            QuantityError = "Enter quantity > 0.";
            SetError("Enter a valid quantity.");
            return;
        }

        if (SelectedInventoryItem.DailyRate <= 0)
        {
            ItemError = "Selected item has no daily rate. Set price in Inventory.";
            SetError(ItemError);
            return;
        }

        if (!int.TryParse(RentalDaysText, out var days) || days <= 0)
        {
            days = 1;
        }

        var alreadyInCart = CartItems
            .Where(c => c.InventoryItemId == SelectedInventoryItem.Id)
            .Sum(c => c.Quantity);
        if (alreadyInCart + qty > SelectedInventoryItem.AvailableQuantity)
        {
            ItemError = $"Only {SelectedInventoryItem.AvailableQuantity} available (cart already has {alreadyInCart}).";
            SetError(ItemError);
            return;
        }

        var existing = CartItems.FirstOrDefault(c => c.InventoryItemId == SelectedInventoryItem.Id);
        if (existing is not null)
        {
            existing.Quantity += qty;
            existing.LineTotal = existing.DailyRate * existing.Quantity * days;
            // refresh collection for UI
            var idx = CartItems.IndexOf(existing);
            CartItems.RemoveAt(idx);
            CartItems.Insert(idx, existing);
        }
        else
        {
            CartItems.Add(new BookingCartItem
            {
                InventoryItemId = SelectedInventoryItem.Id,
                ItemName = SelectedInventoryItem.Name,
                Quantity = qty,
                DailyRate = SelectedInventoryItem.DailyRate,
                LineTotal = SelectedInventoryItem.DailyRate * qty * days
            });
        }

        QuantityText = "1";
        RecalculateAmount();
        SetSuccess($"Added {SelectedInventoryItem.Name}. Cart: {CartItems.Count} line(s).");
    }

    [RelayCommand]
    private void RemoveCartItem(BookingCartItem? item)
    {
        if (item is null)
        {
            return;
        }

        CartItems.Remove(item);
        RecalculateAmount();
    }

    [RelayCommand]
    private void ClearCart()
    {
        CartItems.Clear();
        RecalculateAmount();
    }

    [RelayCommand]
    private async Task AddCustomerQuickAsync()
    {
        if (!ValidationHelper.IsValidName(NewCustomerName))
        {
            SetError("Enter a valid customer name (min 3 letters).");
            return;
        }

        if (!ValidationHelper.IsValidPhone(NewCustomerPhone))
        {
            SetError("Enter a valid customer phone.");
            return;
        }

        var customer = new Customer
        {
            Name = NewCustomerName.Trim(),
            Phone = NewCustomerPhone.Trim()
        };
        await _customers.AddAsync(customer);
        NewCustomerName = string.Empty;
        NewCustomerPhone = string.Empty;
        await LoadAsync();
        SelectedCustomer = Customers.FirstOrDefault(x => x.Phone == customer.Phone);
        SetSuccess($"Customer {customer.Name} added.");
    }

    [RelayCommand]
    private void PayFullAsAdvance()
    {
        if (decimal.TryParse(AmountText, out var total) && total > 0)
        {
            AdvanceText = total.ToString("0");
        }
    }

    [RelayCommand]
    private async Task CreateBookingAsync()
    {
        if (!await _access.EnsureAccessAsync())
        {
            return;
        }

        ClearErrors();
        var valid = true;

        if (SelectedCustomer is null)
        {
            CustomerError = "Select a customer.";
            valid = false;
        }

        if (CartItems.Count == 0)
        {
            ItemError = "Add at least one item (Add item button).";
            valid = false;
        }

        if (!ValidationHelper.IsValidPositiveInt(RentalDaysText, out var days))
        {
            DaysError = "Enter rental days > 0.";
            valid = false;
        }

        if (!BookingDate.HasValue || !ReturnDate.HasValue)
        {
            DateError = "Select booking and return dates.";
            valid = false;
        }
        else if (ReturnDate.Value.Date < BookingDate.Value.Date)
        {
            DateError = "Return date cannot be before booking date.";
            valid = false;
        }

        decimal advance = 0;
        if (!string.IsNullOrWhiteSpace(AdvanceText) && AdvanceText.Trim() != "0")
        {
            if (!decimal.TryParse(AdvanceText.Trim(), out advance) || advance < 0)
            {
                AdvanceError = "Enter 0 or a valid advance amount.";
                valid = false;
            }
        }
        else if (!decimal.TryParse(string.IsNullOrWhiteSpace(AdvanceText) ? "0" : AdvanceText, out advance) || advance < 0)
        {
            AdvanceError = "Enter 0 or a valid advance amount.";
            valid = false;
        }

        if (valid && decimal.TryParse(AmountText, out var total) && advance > total)
        {
            AdvanceError = $"Advance cannot exceed total ₹{total:N0}.";
            valid = false;
        }

        if (!valid)
        {
            SetError("Please fix booking form errors.");
            return;
        }

        try
        {
            var lines = CartItems
                .Select(c => (c.InventoryItemId, c.Quantity))
                .ToList();

            var created = await _operations.CreateBookingAsync(
                SelectedCustomer!.Id,
                lines,
                days,
                BookingDate!.Value,
                ReturnDate!.Value,
                advance);

            CartItems.Clear();
            QuantityText = "1";
            RentalDaysText = "1";
            AdvanceText = "0";
            BookingDate = DateTime.Today;
            ReturnDate = DateTime.Today.AddDays(1);

            var msg = advance > 0
                ? $"Booking {created.BookingNumber} created with {created.Lines.Count} item line(s). Advance ₹{advance:N0}. Balance ₹{created.BalanceDue:N0}. Go to Dispatch."
                : $"Booking {created.BookingNumber} created with {created.Lines.Count} item line(s). Balance ₹{created.BalanceDue:N0}. Go to Dispatch.";
            SetSuccess(msg);
            await LoadAsync();
            SelectedBooking = Bookings.FirstOrDefault(x => x.Id == created.Id);
        }
        catch (Exception ex)
        {
            SetError(ex.Message);
        }
    }

    private void SyncDaysFromDates()
    {
        if (!BookingDate.HasValue || !ReturnDate.HasValue)
        {
            return;
        }

        var days = (int)(ReturnDate.Value.Date - BookingDate.Value.Date).TotalDays;
        if (days < 1)
        {
            days = 1;
        }

        _syncingDaysFromDates = true;
        RentalDaysText = days.ToString();
        _syncingDaysFromDates = false;
        RefreshCartLineTotals(days);
    }

    private void SyncReturnDateFromDays()
    {
        if (!BookingDate.HasValue)
        {
            return;
        }

        if (!int.TryParse(RentalDaysText, out var days) || days <= 0)
        {
            return;
        }

        ReturnDate = BookingDate.Value.Date.AddDays(days);
        RefreshCartLineTotals(days);
    }

    private void RefreshCartLineTotals(int days)
    {
        if (CartItems.Count == 0 || days <= 0)
        {
            return;
        }

        var snapshot = CartItems.ToList();
        CartItems.Clear();
        foreach (var c in snapshot)
        {
            c.LineTotal = c.DailyRate * c.Quantity * days;
            CartItems.Add(c);
        }
    }

    private void UpdateRateHint()
    {
        if (SelectedInventoryItem is null)
        {
            RateHint = "Set daily rate in Inventory Stock.";
            return;
        }

        RateHint = $"Rate ₹{SelectedInventoryItem.DailyRate:N0}/day · Available {SelectedInventoryItem.AvailableQuantity}";
    }

    private void RecalculateAmount()
    {
        UpdateRateHint();
        CartCountText = $"{CartItems.Count} item line(s)";

        if (!int.TryParse(RentalDaysText, out var days) || days <= 0)
        {
            AmountText = string.Empty;
            BalancePreview = string.Empty;
            return;
        }

        RefreshCartLineTotals(days);
        var total = CartItems.Sum(c => c.LineTotal);
        AmountText = total > 0 ? total.ToString("0") : string.Empty;

        decimal.TryParse(AdvanceText, out var advance);
        if (advance < 0)
        {
            advance = 0;
        }

        if (total <= 0)
        {
            BalancePreview = "Add items with “Add item”, then create booking.";
            return;
        }

        var balance = Math.Max(0, total - advance);
        BalancePreview = advance <= 0
            ? $"No advance → collect full ₹{total:N0} later (Receive / Payments)"
            : advance >= total
                ? "Full amount as advance — nothing left later"
                : $"After advance → remaining balance ₹{balance:N0}";
    }

    private void ClearErrors()
    {
        CustomerError = ItemError = QuantityError = DaysError = DateError = AdvanceError = string.Empty;
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
