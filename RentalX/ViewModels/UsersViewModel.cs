using System.Collections.ObjectModel;
using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RentalX.Data.Repositories;
using RentalX.Helpers;
using RentalX.Models;
using RentalX.ViewModels.Base;

namespace RentalX.ViewModels;

public partial class UsersViewModel : PageViewModelBase
{
    private readonly IUserRepository _users;
    private List<AppUser> _all = [];

    public UsersViewModel(IUserRepository users)
    {
        _users = users;
        AvailableRoles =
        [
            "Administrator",
            "Manager",
            "Billing Staff",
            "Dispatch Staff",
            "Warehouse Staff",
            "Field Staff"
        ];
        AvailableDepartments =
        [
            "Management",
            "Accounts",
            "Logistics",
            "Inventory",
            "Operations",
            "Support"
        ];
        SelectedRole = AvailableRoles[2];
        SelectedDepartment = AvailableDepartments[1];
        FormMessageBrush = Brushes.Transparent;
    }

    public ObservableCollection<AppUser> Users { get; } = new();
    public IReadOnlyList<string> AvailableRoles { get; }
    public IReadOnlyList<string> AvailableDepartments { get; }

    [ObservableProperty] private string _newFullName = string.Empty;
    [ObservableProperty] private string _newEmail = string.Empty;
    [ObservableProperty] private string _newPhone = string.Empty;
    [ObservableProperty] private string _selectedRole = "Billing Staff";
    [ObservableProperty] private string _selectedDepartment = "Accounts";
    [ObservableProperty] private AppUser? _selectedStaff;
    [ObservableProperty] private string _searchText = string.Empty;
    [ObservableProperty] private string _resultCountText = "0 staff";
    [ObservableProperty] private string _formMessage = string.Empty;
    [ObservableProperty] private Brush _formMessageBrush = Brushes.Transparent;
    [ObservableProperty] private string _nameError = string.Empty;
    [ObservableProperty] private string _phoneError = string.Empty;
    [ObservableProperty] private string _emailError = string.Empty;
    [ObservableProperty] private string _roleError = string.Empty;
    [ObservableProperty] private string _departmentError = string.Empty;

    partial void OnSearchTextChanged(string value) => ApplyFilter();

    public override async Task LoadAsync()
    {
        IsBusy = true;
        _all = await _users.GetAllAsync();
        ApplyFilter();
        IsBusy = false;
    }

    private void ApplyFilter()
    {
        var filtered = _all.Where(u =>
            ListFilterHelper.Matches(SearchText, u.FullName, u.Email, u.Phone, u.Role, u.Department));
        ListFilterHelper.Replace(Users, filtered);
        ResultCountText = $"{Users.Count} staff";
    }

    [RelayCommand]
    private void ClearSearch() => SearchText = string.Empty;

    [RelayCommand]
    private async Task AddStaffAsync()
    {
        ClearFieldErrors();

        if (!TryValidateStaffForm(out var error))
        {
            SetError(error);
            return;
        }

        if (!string.IsNullOrWhiteSpace(NewEmail) && await _users.EmailExistsAsync(NewEmail))
        {
            EmailError = "This email is already registered.";
            SetError("Please fix the highlighted fields.");
            return;
        }

        if (await _users.PhoneExistsAsync(NewPhone))
        {
            PhoneError = "This phone number is already registered.";
            SetError("Please fix the highlighted fields.");
            return;
        }

        var staff = new AppUser
        {
            FullName = NewFullName.Trim(),
            Email = NewEmail.Trim().ToLowerInvariant(),
            Phone = NewPhone.Trim(),
            Role = SelectedRole,
            Department = SelectedDepartment,
            IsActive = true
        };

        await _users.AddAsync(staff);
        ClearForm();
        SetSuccess($"{staff.FullName} added successfully.");
        await LoadAsync();
    }

    [RelayCommand]
    private async Task AddMyselfAsync()
    {
        NewFullName = Environment.UserName.Replace('.', ' ');
        if (NewFullName.Length < 3)
        {
            NewFullName = "My Staff Profile";
        }

        NewEmail = string.Empty;
        NewPhone = $"9{DateTime.Now:HHmmssfff}"[..10];
        SelectedRole = "Manager";
        SelectedDepartment = "Operations";
        await AddStaffAsync();
    }

    [RelayCommand]
    private async Task ToggleActiveAsync()
    {
        if (SelectedStaff is null)
        {
            SetError("Select a staff row first.");
            return;
        }

        var next = !SelectedStaff.IsActive;
        await _users.SetActiveAsync(SelectedStaff.Id, next);
        SetSuccess($"{SelectedStaff.FullName} marked as {(next ? "Active" : "Inactive")}.");
        await LoadAsync();
    }

    private bool TryValidateStaffForm(out string error)
    {
        var hasError = false;

        if (!ValidationHelper.IsValidName(NewFullName))
        {
            NameError = "Enter full name (min 3 letters).";
            hasError = true;
        }

        if (!ValidationHelper.IsValidPhone(NewPhone))
        {
            PhoneError = "Enter a valid phone (10–15 digits).";
            hasError = true;
        }

        if (!ValidationHelper.IsValidOptionalEmail(NewEmail))
        {
            EmailError = "Email is optional, but must be valid if entered.";
            hasError = true;
        }

        if (!ValidationHelper.IsValidSelection(SelectedRole, AvailableRoles))
        {
            RoleError = "Select a valid role.";
            hasError = true;
        }

        if (!ValidationHelper.IsValidSelection(SelectedDepartment, AvailableDepartments))
        {
            DepartmentError = "Select a valid department.";
            hasError = true;
        }

        error = hasError ? "Name, phone, role and department are required." : string.Empty;
        return !hasError;
    }

    private void ClearFieldErrors()
    {
        NameError = PhoneError = EmailError = RoleError = DepartmentError = string.Empty;
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

    private void ClearForm()
    {
        NewFullName = string.Empty;
        NewEmail = string.Empty;
        NewPhone = string.Empty;
        SelectedRole = AvailableRoles[2];
        SelectedDepartment = AvailableDepartments[1];
        ClearFieldErrors();
    }
}
