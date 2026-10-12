using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RentalX.Data.Repositories;
using RentalX.Models;
using RentalX.Services;
using RentalX.Services.Licensing;

namespace RentalX.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly INavigationService _navigation;
    private readonly IUserRepository _users;
    private readonly ISearchService _search;
    private readonly INotificationService _notifications;
    private readonly ISettingsRepository _settings;
    private readonly ISessionService _session;
    private readonly IClientAccessMonitor _access;
    private readonly List<NavigationItem> _allNavItems;

    public MainViewModel(
        INavigationService navigation,
        IUserRepository users,
        ISearchService search,
        INotificationService notifications,
        ISettingsRepository settings,
        ISessionService session,
        IClientAccessMonitor access)
    {
        _navigation = navigation;
        _users = users;
        _search = search;
        _notifications = notifications;
        _settings = settings;
        _session = session;
        _access = access;
        _allNavItems = CreateNavigationItems().ToList();

        MainNavItems = new ObservableCollection<NavigationItem>();
        BusinessNavItems = new ObservableCollection<NavigationItem>();
        SystemNavItems = new ObservableCollection<NavigationItem>();
        SearchResults = new ObservableCollection<SearchResultItem>();
        Notifications = new ObservableCollection<AppNotification>();

        _navigation.Navigated += OnNavigated;
        _navigation.LoadingChanged += OnLoadingChanged;
        _ = InitializeAsync();
    }

    public ObservableCollection<NavigationItem> MainNavItems { get; }
    public ObservableCollection<NavigationItem> BusinessNavItems { get; }
    public ObservableCollection<NavigationItem> SystemNavItems { get; }
    public ObservableCollection<SearchResultItem> SearchResults { get; }
    public ObservableCollection<AppNotification> Notifications { get; }

    [ObservableProperty] private ObservableObject? _currentViewModel;
    [ObservableProperty] private string _pageTitle = "Dashboard";
    [ObservableProperty] private string _pageSubtitle = "Overview of your rental business";
    [ObservableProperty] private string _searchText = string.Empty;
    [ObservableProperty] private string _currentUserName = "Admin User";
    [ObservableProperty] private string _currentUserRole = "Administrator";
    [ObservableProperty] private string _currentUserInitial = "A";
    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private string _loadingMessage = "Loading...";
    [ObservableProperty] private bool _isSearchOpen;
    [ObservableProperty] private bool _isNotificationsOpen;
    [ObservableProperty] private int _notificationCount;
    [ObservableProperty] private bool _hasNotifications;
    [ObservableProperty] private string _companyDisplayName = "RentalX";

    partial void OnSearchTextChanged(string value) => _ = RunSearchAsync(value);

    private async Task InitializeAsync()
    {
        await LoadUserAsync();
        await RefreshNotificationsAsync();
        await _navigation.NavigateToAsync("Dashboard");
    }

    [RelayCommand]
    private async Task NavigateAsync(string pageKey)
    {
        if (string.IsNullOrWhiteSpace(pageKey) || IsLoading)
        {
            return;
        }

        IsSearchOpen = false;
        IsNotificationsOpen = false;

        if (pageKey == _navigation.CurrentPageKey && CurrentViewModel is not null)
        {
            return;
        }

        await _navigation.NavigateToAsync(pageKey);
    }

    [RelayCommand]
    private async Task NewBookingAsync()
    {
        if (!await _access.EnsureAccessAsync())
        {
            return;
        }

        await _navigation.NavigateToAsync("Bookings");
    }

    [RelayCommand]
    private void CloseApplication() => Application.Current.Shutdown();

    [RelayCommand]
    private void Logout()
    {
        var confirm = MessageBox.Show(
            "Sign out and return to the login screen?",
            "Sign Out",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);
        if (confirm != MessageBoxResult.Yes)
        {
            return;
        }

        _session.SignOut();
    }

    [RelayCommand]
    private async Task ToggleNotificationsAsync()
    {
        IsSearchOpen = false;
        IsNotificationsOpen = !IsNotificationsOpen;
        if (IsNotificationsOpen)
        {
            await RefreshNotificationsAsync();
        }
    }

    [RelayCommand]
    private async Task OpenSearchResultAsync(SearchResultItem? item)
    {
        if (item is null)
        {
            return;
        }

        SearchText = string.Empty;
        IsSearchOpen = false;
        await _navigation.NavigateToAsync(item.PageKey);
    }

    private async Task RunSearchAsync(string value)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Trim().Length < 2)
        {
            SearchResults.Clear();
            IsSearchOpen = false;
            return;
        }

        var results = await _search.SearchAsync(value);
        SearchResults.Clear();
        foreach (var item in results)
        {
            SearchResults.Add(item);
        }

        IsNotificationsOpen = false;
        IsSearchOpen = SearchResults.Count > 0;
    }

    private async Task RefreshNotificationsAsync()
    {
        var settings = await _settings.GetAsync();
        CompanyDisplayName = settings.CompanyName;

        if (!settings.EnableNotifications)
        {
            Notifications.Clear();
            NotificationCount = 0;
            HasNotifications = false;
            return;
        }

        var items = await _notifications.GetNotificationsAsync();
        Notifications.Clear();
        foreach (var item in items)
        {
            Notifications.Add(item);
        }

        NotificationCount = Notifications.Count(x => x.Type is "Danger" or "Warning");
        HasNotifications = NotificationCount > 0;
    }

    private void OnLoadingChanged(bool isLoading, string message)
    {
        IsLoading = isLoading;
        if (!string.IsNullOrWhiteSpace(message))
        {
            LoadingMessage = message;
        }
    }

    private async Task LoadUserAsync()
    {
        if (_session.CurrentAccount is not null)
        {
            var account = _session.CurrentAccount;
            CurrentUserName = string.IsNullOrWhiteSpace(account.DisplayName) ? account.Username : account.DisplayName;
            CurrentUserRole = $"Login: {account.Username}";
            CurrentUserInitial = CurrentUserName[..1].ToUpperInvariant();
            return;
        }

        var admin = await _users.GetAdminAsync();
        if (admin is null)
        {
            return;
        }

        CurrentUserName = admin.FullName;
        CurrentUserRole = admin.Role;
        CurrentUserInitial = string.IsNullOrWhiteSpace(admin.FullName)
            ? "A"
            : admin.FullName[..1].ToUpperInvariant();
    }

    private void OnNavigated()
    {
        CurrentViewModel = _navigation.CurrentViewModel;
        PageTitle = _navigation.CurrentPageTitle;
        PageSubtitle = _navigation.CurrentPageSubtitle;
        RefreshNavigationSelection();
        _ = RefreshNotificationsAsync();
    }

    private void RefreshNavigationSelection()
    {
        FillGroup(MainNavItems, "MAIN");
        FillGroup(BusinessNavItems, "BUSINESS");
        FillGroup(SystemNavItems, "SYSTEM");
    }

    private void FillGroup(ObservableCollection<NavigationItem> target, string group)
    {
        target.Clear();
        foreach (var item in _allNavItems.Where(x => x.Group == group))
        {
            target.Add(new NavigationItem
            {
                Key = item.Key,
                Title = item.Title,
                Icon = item.Icon,
                Group = item.Group,
                IsSelected = item.Key == _navigation.CurrentPageKey
            });
        }
    }

    private static IEnumerable<NavigationItem> CreateNavigationItems() =>
    [
        new() { Key = "Dashboard", Title = "Dashboard", Icon = "\uE80F", Group = "MAIN" },
        new() { Key = "Bookings", Title = "1. Bookings", Icon = "\uE787", Group = "MAIN" },
        new() { Key = "Inventory", Title = "Inventory", Icon = "\uE7B8", Group = "MAIN" },
        new() { Key = "Dispatch", Title = "2. Dispatch", Icon = "\uE724", Group = "MAIN" },
        new() { Key = "Returns", Title = "3. Receive", Icon = "\uE7A7", Group = "MAIN" },
        new() { Key = "Billing", Title = "Billing", Icon = "\uE8C7", Group = "BUSINESS" },
        new() { Key = "Payments", Title = "Payments", Icon = "\uE8CB", Group = "BUSINESS" },
        new() { Key = "Expenses", Title = "Expenses", Icon = "\uE8C8", Group = "BUSINESS" },
        new() { Key = "Maintenance", Title = "Maintenance", Icon = "\uE90F", Group = "BUSINESS" },
        new() { Key = "Customers", Title = "Customers", Icon = "\uE716", Group = "BUSINESS" },
        new() { Key = "Reports", Title = "Reports", Icon = "\uE9F9", Group = "BUSINESS" },
        new() { Key = "Settings", Title = "Settings", Icon = "\uE713", Group = "SYSTEM" },
        new() { Key = "Users", Title = "Staff", Icon = "\uE8D7", Group = "SYSTEM" }
    ];
}
