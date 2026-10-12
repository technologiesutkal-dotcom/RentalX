using System.Collections.ObjectModel;
using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RentalX.Data.Repositories;
using RentalX.Helpers;
using RentalX.Models;
using RentalX.ViewModels.Base;

namespace RentalX.ViewModels;

public partial class ExpensesViewModel : PageViewModelBase
{
    private readonly IExpenseRepository _expenses;
    private List<Expense> _all = [];

    public ExpensesViewModel(IExpenseRepository expenses)
    {
        _expenses = expenses;
        AvailableCategories =
        [
            "Transport",
            "Maintenance",
            "Rent",
            "Salary",
            "Utilities",
            "Purchase",
            "General"
        ];
        FilterCategories = ["All", .. AvailableCategories];
        SelectedCategory = AvailableCategories[0];
        FilterCategory = "All";
        ExpenseDate = DateTime.Today;
    }

    public ObservableCollection<Expense> Expenses { get; } = new();
    public IReadOnlyList<string> AvailableCategories { get; }
    public IReadOnlyList<string> FilterCategories { get; }

    [ObservableProperty] private string _title = string.Empty;
    [ObservableProperty] private string _selectedCategory = "General";
    [ObservableProperty] private string _amountText = string.Empty;
    [ObservableProperty] private DateTime? _expenseDate;
    [ObservableProperty] private string _notes = string.Empty;
    [ObservableProperty] private Expense? _selectedExpense;
    [ObservableProperty] private string _searchText = string.Empty;
    [ObservableProperty] private string _filterCategory = "All";
    [ObservableProperty] private string _resultCountText = "0";
    [ObservableProperty] private string _formMessage = string.Empty;
    [ObservableProperty] private Brush _formMessageBrush = Brushes.Transparent;
    [ObservableProperty] private string _titleError = string.Empty;
    [ObservableProperty] private string _amountError = string.Empty;
    [ObservableProperty] private string _dateError = string.Empty;
    [ObservableProperty] private string _categoryError = string.Empty;
    [ObservableProperty] private string _totalText = "₹0";

    partial void OnSearchTextChanged(string value) => ApplyFilter();
    partial void OnFilterCategoryChanged(string value) => ApplyFilter();

    public override async Task LoadAsync()
    {
        IsBusy = true;
        _all = await _expenses.GetAllAsync();
        ApplyFilter();
        IsBusy = false;
    }

    private void ApplyFilter()
    {
        var filtered = _all.Where(e =>
            (FilterCategory == "All" || string.Equals(e.Category, FilterCategory, StringComparison.OrdinalIgnoreCase)) &&
            ListFilterHelper.Matches(SearchText, e.Title, e.Category, e.Notes, e.AmountText, e.ExpenseDateText));
        ListFilterHelper.Replace(Expenses, filtered);
        TotalText = $"₹{Expenses.Sum(x => x.Amount):N0}";
        ResultCountText = $"{Expenses.Count} expense(s)";
    }

    [RelayCommand]
    private void ClearSearch()
    {
        SearchText = string.Empty;
        FilterCategory = "All";
    }

    [RelayCommand]
    private async Task AddExpenseAsync()
    {
        TitleError = AmountError = DateError = CategoryError = string.Empty;
        var valid = true;

        if (!ValidationHelper.IsRequired(Title) || Title.Trim().Length < 3)
        {
            TitleError = "Enter expense title (min 3 characters).";
            valid = false;
        }

        if (!ValidationHelper.IsValidSelection(SelectedCategory, AvailableCategories))
        {
            CategoryError = "Select category.";
            valid = false;
        }

        if (!ValidationHelper.IsValidPositiveDecimal(AmountText, out var amount))
        {
            AmountError = "Enter a valid amount.";
            valid = false;
        }

        if (!ExpenseDate.HasValue)
        {
            DateError = "Select expense date.";
            valid = false;
        }
        else if (ExpenseDate.Value.Date > DateTime.Today)
        {
            DateError = "Future dates are not allowed.";
            valid = false;
        }

        if (!valid)
        {
            FormMessage = "Please fix expense form errors.";
            FormMessageBrush = new SolidColorBrush(Color.FromRgb(220, 38, 38));
            return;
        }

        await _expenses.AddAsync(new Expense
        {
            Title = Title.Trim(),
            Category = SelectedCategory,
            Amount = amount,
            ExpenseDate = ExpenseDate!.Value.Date,
            Notes = Notes.Trim()
        });

        Title = string.Empty;
        AmountText = string.Empty;
        Notes = string.Empty;
        ExpenseDate = DateTime.Today;
        SelectedCategory = AvailableCategories[0];
        FormMessage = "Expense added successfully.";
        FormMessageBrush = new SolidColorBrush(Color.FromRgb(5, 150, 105));
        await LoadAsync();
    }
}
