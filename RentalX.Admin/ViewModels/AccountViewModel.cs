using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RentalX.Admin.Models;
using RentalX.Admin.Services;

namespace RentalX.Admin.ViewModels;

public partial class AccountViewModel : ObservableObject
{
    private readonly ILicenseApiClient _api;
    private readonly IAdminSession _session;

    public AccountViewModel(ILicenseApiClient api, IAdminSession session)
    {
        _api = api;
        _session = session;
        CurrentUsername = session.Username ?? "superadmin";
        NewUsername = CurrentUsername;
        _ = LoadAdminSerialsAsync();
    }

    public ObservableCollection<AdminSerialDto> AdminSerials { get; } = new();

    [ObservableProperty] private string _currentUsername = "superadmin";
    [ObservableProperty] private string _newUsername = string.Empty;
    [ObservableProperty] private string _currentPassword = string.Empty;
    [ObservableProperty] private string _newPassword = string.Empty;
    [ObservableProperty] private string _confirmPassword = string.Empty;
    [ObservableProperty] private bool _showPasswords;
    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private string _message = string.Empty;
    [ObservableProperty] private Brush _messageBrush = Brushes.Transparent;

    [ObservableProperty] private string _newAdminSerial = string.Empty;
    [ObservableProperty] private string _newAdminMachine = string.Empty;
    [ObservableProperty] private string _newAdminNotes = string.Empty;
    [ObservableProperty] private AdminSerialDto? _selectedAdminSerial;
    [ObservableProperty] private string _serialMessage = string.Empty;
    [ObservableProperty] private Brush _serialMessageBrush = Brushes.Transparent;

    [RelayCommand]
    private async Task LoadAdminSerialsAsync()
    {
        var (data, error) = await _api.GetAdminSerialsAsync();
        AdminSerials.Clear();
        if (data is null)
        {
            SetSerialError(error ?? "Could not load Super Admin laptops.");
            return;
        }

        foreach (var row in data)
        {
            AdminSerials.Add(row);
        }
    }

    [RelayCommand]
    private async Task AddAdminSerialAsync()
    {
        if (string.IsNullOrWhiteSpace(NewAdminSerial) || NewAdminSerial.Trim().Length < 8)
        {
            SetSerialError("Paste the laptop serial from that PC's login screen (Copy serial).");
            return;
        }

        IsBusy = true;
        try
        {
            var (data, error) = await _api.AddAdminSerialAsync(new AdminSerialUpsertRequest
            {
                SerialNumber = NewAdminSerial.Trim(),
                MachineName = string.IsNullOrWhiteSpace(NewAdminMachine) ? null : NewAdminMachine.Trim(),
                Notes = string.IsNullOrWhiteSpace(NewAdminNotes) ? null : NewAdminNotes.Trim()
            });
            if (data is null)
            {
                SetSerialError(error ?? "Could not add serial.");
                return;
            }

            NewAdminSerial = string.Empty;
            NewAdminMachine = string.Empty;
            NewAdminNotes = string.Empty;
            SerialMessage = $"Authorized: {data.DisplaySerial}. That laptop can now sign in with the same Super Admin username/password.";
            SerialMessageBrush = new SolidColorBrush(Color.FromRgb(5, 150, 105));
            await LoadAdminSerialsAsync();
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task RemoveAdminSerialAsync()
    {
        if (SelectedAdminSerial is null)
        {
            SetSerialError("Select a laptop serial to remove.");
            return;
        }

        var confirm = MessageBox.Show(
            $"Remove Super Admin access for:\n{SelectedAdminSerial.DisplaySerial}\n\nThat PC will no longer be able to sign in as Super Admin.",
            "Remove Super Admin laptop",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);
        if (confirm != MessageBoxResult.Yes)
        {
            return;
        }

        IsBusy = true;
        try
        {
            var (message, error) = await _api.RemoveAdminSerialAsync(SelectedAdminSerial.Id);
            if (error is not null)
            {
                SetSerialError(error);
                return;
            }

            SerialMessage = message ?? "Removed.";
            SerialMessageBrush = new SolidColorBrush(Color.FromRgb(5, 150, 105));
            SelectedAdminSerial = null;
            await LoadAdminSerialsAsync();
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        Message = string.Empty;
        if (string.IsNullOrWhiteSpace(NewUsername) || NewUsername.Trim().Length < 3)
        {
            SetError("Username must be at least 3 characters.");
            return;
        }

        if (string.IsNullOrWhiteSpace(CurrentPassword))
        {
            SetError("Enter your current password.");
            return;
        }

        if (string.IsNullOrWhiteSpace(NewPassword) || NewPassword.Length < 6)
        {
            SetError("New password must be at least 6 characters.");
            return;
        }

        if (NewPassword != ConfirmPassword)
        {
            SetError("New password and confirm password do not match.");
            return;
        }

        IsBusy = true;
        try
        {
            var (username, error) = await _api.ChangeAdminCredentialsAsync(
                new ChangeAdminCredentialsRequest
                {
                    CurrentPassword = CurrentPassword,
                    NewUsername = NewUsername.Trim(),
                    NewPassword = NewPassword
                });

            if (error is not null || string.IsNullOrWhiteSpace(username))
            {
                SetError(error ?? "Could not update credentials.");
                return;
            }

            CurrentUsername = username;
            NewUsername = username;
            _session.UpdateUsername(username);
            CurrentPassword = NewPassword = ConfirmPassword = string.Empty;
            Message = $"Saved. Next login use Username: {username}";
            MessageBrush = new SolidColorBrush(Color.FromRgb(5, 150, 105));
            MessageBox.Show(
                $"Super Admin credentials updated.\n\nUsername: {username}\n\nUse these next time you sign in.",
                "Account",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void SetError(string text)
    {
        Message = text;
        MessageBrush = new SolidColorBrush(Color.FromRgb(220, 38, 38));
    }

    private void SetSerialError(string text)
    {
        SerialMessage = text;
        SerialMessageBrush = new SolidColorBrush(Color.FromRgb(220, 38, 38));
    }
}
