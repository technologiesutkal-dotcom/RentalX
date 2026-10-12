using System.Windows;
using RentalX.ViewModels;

namespace RentalX.Views;

public partial class LoginWindow : Window
{
    public LoginWindow(LoginViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        viewModel.LoginSucceeded += OnLoginSucceeded;
        Closed += (_, _) => viewModel.LoginSucceeded -= OnLoginSucceeded;
    }

    private void OnLoginSucceeded()
    {
        try
        {
            DialogResult = true;
        }
        catch
        {
            // not shown as dialog
        }

        Close();
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            DialogResult = false;
        }
        catch
        {
            // Window may not be a dialog in some hosts
        }

        Close();
    }
}
