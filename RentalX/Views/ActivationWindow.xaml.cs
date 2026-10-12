using System.Windows;
using RentalX.Services.Licensing;
using RentalX.ViewModels;

namespace RentalX.Views;

public partial class ActivationWindow : Window
{
    public ActivationWindow(ILicenseClientService license, IMachineIdService machine)
    {
        InitializeComponent();
        DataContext = new ActivationViewModel(license, machine, this);
    }
}
