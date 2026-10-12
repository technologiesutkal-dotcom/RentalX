using CommunityToolkit.Mvvm.ComponentModel;

namespace RentalX.Services;

public interface INavigationService
{
    ObservableObject? CurrentViewModel { get; }
    string CurrentPageKey { get; }
    string CurrentPageTitle { get; }
    string CurrentPageSubtitle { get; }
    event Action? Navigated;
    event Action<bool, string>? LoadingChanged;
    Task NavigateToAsync(string pageKey);
}
