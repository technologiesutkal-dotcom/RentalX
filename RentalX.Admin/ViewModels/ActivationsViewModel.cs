using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RentalX.Admin.Models;
using RentalX.Admin.Services;

namespace RentalX.Admin.ViewModels;

public partial class ActivationsViewModel : ObservableObject
{
    private readonly ILicenseApiClient _api;

    [ObservableProperty] private ObservableCollection<ActivationDto> _activations = [];
    [ObservableProperty] private ActivationDto? _selectedActivation;
    [ObservableProperty] private string _searchText = string.Empty;
    [ObservableProperty] private bool _activeOnly = true;
    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private string? _errorMessage;
    [ObservableProperty] private string? _successMessage;
    [ObservableProperty] private string _deactivateReason = string.Empty;

    public ActivationsViewModel(ILicenseApiClient api)
    {
        _api = api;
        _ = LoadAsync();
    }

    partial void OnSearchTextChanged(string value) => _ = LoadAsync();
    partial void OnActiveOnlyChanged(bool value) => _ = LoadAsync();

    [RelayCommand]
    private async Task LoadAsync()
    {
        IsBusy = true;
        ErrorMessage = null;
        try
        {
            var (data, error) = await _api.GetActivationsAsync(SearchText, ActiveOnly ? true : null);
            if (data is null)
            {
                ErrorMessage = error;
                return;
            }

            Activations = new ObservableCollection<ActivationDto>(data);
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task DeactivateAsync()
    {
        if (SelectedActivation is null)
        {
            ErrorMessage = "Select an activation first.";
            return;
        }

        if (!SelectedActivation.IsActive)
        {
            ErrorMessage = "This activation is already inactive.";
            return;
        }

        var confirm = MessageBox.Show(
            $"Deactivate machine {SelectedActivation.MachineName ?? SelectedActivation.MachineId}?",
            "Deactivate",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);
        if (confirm != MessageBoxResult.Yes)
        {
            return;
        }

        IsBusy = true;
        ErrorMessage = null;
        try
        {
            var (data, error) = await _api.DeactivateAsync(
                SelectedActivation.Id,
                string.IsNullOrWhiteSpace(DeactivateReason) ? null : DeactivateReason.Trim());
            if (data is null)
            {
                ErrorMessage = error;
                return;
            }

            SuccessMessage = "Machine deactivated.";
            DeactivateReason = string.Empty;
            await LoadAsync();
        }
        finally
        {
            IsBusy = false;
        }
    }
}
