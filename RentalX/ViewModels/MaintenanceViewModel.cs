using System.Collections.ObjectModel;
using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RentalX.Data.Repositories;
using RentalX.Helpers;
using RentalX.Models;
using RentalX.ViewModels.Base;

namespace RentalX.ViewModels;

public partial class MaintenanceViewModel : PageViewModelBase
{
    private readonly IInventoryRepository _inventory;
    private List<InventoryItem> _all = [];

    public MaintenanceViewModel(IInventoryRepository inventory)
    {
        _inventory = inventory;
    }

    public ObservableCollection<InventoryItem> Items { get; } = new();

    [ObservableProperty] private InventoryItem? _selectedItem;
    [ObservableProperty] private string _searchText = string.Empty;
    [ObservableProperty] private string _resultCountText = "0 items";
    [ObservableProperty] private string _formMessage = string.Empty;
    [ObservableProperty] private Brush _formMessageBrush = Brushes.Transparent;

    partial void OnSearchTextChanged(string value) => ApplyFilter();

    public override async Task LoadAsync()
    {
        IsBusy = true;
        var selectedId = SelectedItem?.Id;
        _all = await _inventory.GetAllAsync();
        ApplyFilter();
        SelectedItem = selectedId is null
            ? Items.FirstOrDefault()
            : Items.FirstOrDefault(x => x.Id == selectedId) ?? Items.FirstOrDefault();
        IsBusy = false;
    }

    private void ApplyFilter()
    {
        var filtered = _all.Where(item =>
            ListFilterHelper.Matches(SearchText, item.Name, item.Category, item.Status, item.DailyRate.ToString("0")));
        ListFilterHelper.Replace(Items, filtered.OrderBy(x => x.Name));
        ResultCountText = $"{Items.Count} item(s)";
    }

    [RelayCommand]
    private void ClearSearch() => SearchText = string.Empty;

    [RelayCommand]
    private async Task MarkUnderMaintenanceAsync()
    {
        await SetStatusAsync("Maintenance");
    }

    [RelayCommand]
    private async Task MarkActiveAsync()
    {
        await SetStatusAsync("Active");
    }

    private async Task SetStatusAsync(string status)
    {
        if (SelectedItem is null)
        {
            FormMessage = "Select an inventory item first.";
            FormMessageBrush = new SolidColorBrush(Color.FromRgb(220, 38, 38));
            return;
        }

        try
        {
            await _inventory.UpdateStatusAsync(SelectedItem.Id, status);
            FormMessage = $"{SelectedItem.Name} marked as {status}.";
            FormMessageBrush = new SolidColorBrush(Color.FromRgb(5, 150, 105));
            await LoadAsync();
        }
        catch (Exception ex)
        {
            FormMessage = ex.Message;
            FormMessageBrush = new SolidColorBrush(Color.FromRgb(220, 38, 38));
        }
    }
}
