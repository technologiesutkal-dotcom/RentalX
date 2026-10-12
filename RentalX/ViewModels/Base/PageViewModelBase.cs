using CommunityToolkit.Mvvm.ComponentModel;

namespace RentalX.ViewModels.Base;

public abstract partial class PageViewModelBase : ObservableObject
{
    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    public virtual Task LoadAsync() => Task.CompletedTask;
}
