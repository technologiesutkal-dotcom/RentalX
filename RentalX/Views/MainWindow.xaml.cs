using System.Windows;
using RentalX.ViewModels;

namespace RentalX.Views;

public partial class MainWindow : Window
{
    public MainWindow(MainViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        WindowState = WindowState.Maximized;
    }
}
