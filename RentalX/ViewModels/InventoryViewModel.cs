using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RentalX.Data.Repositories;
using RentalX.Helpers;
using RentalX.Models;
using RentalX.ViewModels.Base;

namespace RentalX.ViewModels;

public partial class InventoryViewModel : PageViewModelBase
{
    private readonly IInventoryRepository _inventory;
    private List<InventoryItem> _allItems = [];

    public InventoryViewModel(IInventoryRepository inventory)
    {
        _inventory = inventory;
        AvailableCategories =
        [
            "Seating",
            "Furniture",
            "Power",
            "Equipment",
            "Media",
            "Decor",
            "Kitchen",
            "General"
        ];
        AvailableStatuses = ["Active", "Maintenance", "Inactive"];
        FilterCategories = ["All", .. AvailableCategories];
        FilterStatuses = ["All", .. AvailableStatuses];
        SelectedCategory = AvailableCategories[0];
        EditCategory = AvailableCategories[0];
        EditStatus = AvailableStatuses[0];
        FilterCategory = "All";
        FilterStatus = "All";
    }

    public ObservableCollection<InventoryItem> Items { get; } = new();
    public IReadOnlyList<string> AvailableCategories { get; }
    public IReadOnlyList<string> AvailableStatuses { get; }
    public IReadOnlyList<string> FilterCategories { get; }
    public IReadOnlyList<string> FilterStatuses { get; }

    [ObservableProperty] private string _newItemName = string.Empty;
    [ObservableProperty] private string _selectedCategory = "Seating";
    [ObservableProperty] private string _newQuantity = string.Empty;
    [ObservableProperty] private string _newDailyRate = string.Empty;
    [ObservableProperty] private string _addStockQuantity = string.Empty;
    [ObservableProperty] private InventoryItem? _selectedItem;
    [ObservableProperty] private string _editName = string.Empty;
    [ObservableProperty] private string _editCategory = "Seating";
    [ObservableProperty] private string _editTotalQuantity = string.Empty;
    [ObservableProperty] private string _editDailyRate = string.Empty;
    [ObservableProperty] private string _editStatus = "Active";
    [ObservableProperty] private string _searchText = string.Empty;
    [ObservableProperty] private string _filterCategory = "All";
    [ObservableProperty] private string _filterStatus = "All";
    [ObservableProperty] private string _resultCountText = "0 items";
    [ObservableProperty] private string _formMessage = string.Empty;
    [ObservableProperty] private Brush _formMessageBrush = Brushes.Transparent;
    [ObservableProperty] private string _nameError = string.Empty;
    [ObservableProperty] private string _categoryError = string.Empty;
    [ObservableProperty] private string _quantityError = string.Empty;
    [ObservableProperty] private string _rateError = string.Empty;
    [ObservableProperty] private string _addStockError = string.Empty;
    [ObservableProperty] private string _editError = string.Empty;

    partial void OnSearchTextChanged(string value) => ApplyFilter();
    partial void OnFilterCategoryChanged(string value) => ApplyFilter();
    partial void OnFilterStatusChanged(string value) => ApplyFilter();

    public override async Task LoadAsync()
    {
        IsBusy = true;
        var selectedId = SelectedItem?.Id;
        _allItems = await _inventory.GetAllAsync();
        ApplyFilter();
        SelectedItem = selectedId is null
            ? Items.FirstOrDefault()
            : Items.FirstOrDefault(x => x.Id == selectedId) ?? Items.FirstOrDefault();
        IsBusy = false;
    }

    private void ApplyFilter()
    {
        var filtered = _allItems.Where(item =>
            (FilterCategory == "All" || string.Equals(item.Category, FilterCategory, StringComparison.OrdinalIgnoreCase)) &&
            (FilterStatus == "All" || string.Equals(item.Status, FilterStatus, StringComparison.OrdinalIgnoreCase)) &&
            ListFilterHelper.Matches(
                SearchText,
                item.Name,
                item.Category,
                item.Status,
                item.DailyRate.ToString("0"),
                item.TotalQuantity.ToString(),
                item.AvailableQuantity.ToString()));

        ListFilterHelper.Replace(Items, filtered.OrderBy(x => x.Name));
        ResultCountText = $"{Items.Count} item(s)";
    }

    partial void OnSelectedItemChanged(InventoryItem? value)
    {
        EditError = string.Empty;
        if (value is null)
        {
            EditName = string.Empty;
            EditCategory = AvailableCategories[0];
            EditTotalQuantity = string.Empty;
            EditDailyRate = string.Empty;
            EditStatus = AvailableStatuses[0];
            return;
        }

        EditName = value.Name;
        EditCategory = AvailableCategories.Contains(value.Category) ? value.Category : AvailableCategories[0];
        EditTotalQuantity = value.TotalQuantity.ToString();
        EditDailyRate = value.DailyRate.ToString("0");
        EditStatus = AvailableStatuses.Contains(value.Status) ? value.Status : AvailableStatuses[0];
    }

    [RelayCommand]
    private async Task AddItemAsync()
    {
        ClearErrors();

        var valid = true;
        if (!ValidationHelper.IsValidItemName(NewItemName))
        {
            NameError = "Enter item name (min 2 characters).";
            valid = false;
        }

        if (!ValidationHelper.IsValidSelection(SelectedCategory, AvailableCategories))
        {
            CategoryError = "Select a category.";
            valid = false;
        }

        if (!ValidationHelper.IsValidPositiveInt(NewQuantity, out var qty))
        {
            QuantityError = "Enter quantity greater than 0.";
            valid = false;
        }

        if (!ValidationHelper.IsValidPositiveDecimal(NewDailyRate, out var rate))
        {
            RateError = "Enter a valid daily rate.";
            valid = false;
        }

        if (!valid)
        {
            SetError("Please fill all required item fields correctly.");
            return;
        }

        if (await _inventory.NameExistsAsync(NewItemName))
        {
            NameError = "Item already exists. Use Add Stock or Edit instead.";
            SetError("Duplicate item name.");
            return;
        }

        await _inventory.AddAsync(new InventoryItem
        {
            Name = NewItemName.Trim(),
            Category = SelectedCategory,
            TotalQuantity = qty,
            RentedQuantity = 0,
            DailyRate = rate,
            Status = "Active"
        });

        NewItemName = string.Empty;
        NewQuantity = string.Empty;
        NewDailyRate = string.Empty;
        SelectedCategory = AvailableCategories[0];
        SetSuccess("Item added successfully.");
        await LoadAsync();
    }

    [RelayCommand]
    private async Task AddStockAsync()
    {
        AddStockError = string.Empty;
        if (SelectedItem is null)
        {
            SetError("Select an inventory row first.");
            return;
        }

        if (!ValidationHelper.IsValidPositiveInt(AddStockQuantity, out var qty))
        {
            AddStockError = "Enter quantity greater than 0.";
            SetError("Invalid stock quantity.");
            return;
        }

        await _inventory.AddStockAsync(SelectedItem.Id, qty);
        AddStockQuantity = string.Empty;
        SetSuccess($"Added {qty} units to {SelectedItem.Name}.");
        await LoadAsync();
    }

    [RelayCommand]
    private async Task SaveSelectedItemAsync()
    {
        EditError = string.Empty;
        if (SelectedItem is null)
        {
            SetError("Select an inventory row to edit.");
            return;
        }

        if (!ValidationHelper.IsValidItemName(EditName))
        {
            EditError = "Enter item name (min 2 characters).";
            SetError("Invalid item name.");
            return;
        }

        if (!ValidationHelper.IsValidSelection(EditCategory, AvailableCategories))
        {
            EditError = "Select a category.";
            SetError("Invalid category.");
            return;
        }

        if (!ValidationHelper.IsValidPositiveInt(EditTotalQuantity, out var totalQty))
        {
            EditError = "Enter total quantity greater than 0.";
            SetError("Invalid total quantity.");
            return;
        }

        if (!ValidationHelper.IsValidPositiveDecimal(EditDailyRate, out var rate))
        {
            EditError = "Enter a valid daily rate / price.";
            SetError("Invalid daily rate.");
            return;
        }

        if (!ValidationHelper.IsValidSelection(EditStatus, AvailableStatuses))
        {
            EditError = "Select a status.";
            SetError("Invalid status.");
            return;
        }

        try
        {
            await _inventory.UpdateItemAsync(new InventoryItem
            {
                Id = SelectedItem.Id,
                Name = EditName.Trim(),
                Category = EditCategory,
                TotalQuantity = totalQty,
                DailyRate = rate,
                Status = EditStatus
            });

            SetSuccess($"Updated {EditName.Trim()} (all fields saved).");
            await LoadAsync();
        }
        catch (Exception ex)
        {
            EditError = ex.Message;
            SetError(ex.Message);
        }
    }

    [RelayCommand]
    private async Task DeleteSelectedItemAsync()
    {
        if (SelectedItem is null)
        {
            SetError("Select an inventory row to delete.");
            return;
        }

        var name = SelectedItem.Name;
        var confirm = MessageBox.Show(
            $"Delete inventory item \"{name}\"?\n\nThis cannot be undone. Items currently rented or on active bookings cannot be deleted.",
            "Delete Inventory Item",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (confirm != MessageBoxResult.Yes)
        {
            return;
        }

        try
        {
            await _inventory.DeleteAsync(SelectedItem.Id);
            SelectedItem = null;
            SetSuccess($"Deleted {name}.");
            await LoadAsync();
        }
        catch (Exception ex)
        {
            EditError = ex.Message;
            SetError(ex.Message);
        }
    }

    [RelayCommand]
    private void ClearSearch()
    {
        SearchText = string.Empty;
        FilterCategory = "All";
        FilterStatus = "All";
    }

    private void ClearErrors()
    {
        NameError = CategoryError = QuantityError = RateError = AddStockError = EditError = string.Empty;
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
