using System.IO;
using System.Net.Http;
using System.Windows;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RentalX.Admin.Services;
using RentalX.Data;
using RentalX.Data.Repositories;
using RentalX.Services;
using RentalX.Services.Licensing;
using RentalX.ViewModels;
using RentalX.Views;
using AdminServiceHelper = RentalX.Admin.Helpers.ServiceHelper;
using ClientServiceHelper = RentalX.Helpers.ServiceHelper;

namespace RentalX;

public partial class App : Application
{
    private ServiceProvider? _services;
    private Window? _activeShell;
    private bool _handlingSignOut;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        ShutdownMode = ShutdownMode.OnExplicitShutdown;

        var dbFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "RentalX");
        Directory.CreateDirectory(dbFolder);
        var dbPath = Path.Combine(dbFolder, "rentalx.db");

        var services = new ServiceCollection();

        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlite($"Data Source={dbPath}"),
            ServiceLifetime.Singleton);

        services.AddSingleton<ICustomerRepository, CustomerRepository>();
        services.AddSingleton<IInventoryRepository, InventoryRepository>();
        services.AddSingleton<IBookingRepository, BookingRepository>();
        services.AddSingleton<IPaymentRepository, PaymentRepository>();
        services.AddSingleton<IDashboardRepository, DashboardRepository>();
        services.AddSingleton<IUserRepository, UserRepository>();
        services.AddSingleton<IExpenseRepository, ExpenseRepository>();
        services.AddSingleton<ISettingsRepository, SettingsRepository>();
        services.AddSingleton<ILoginAccountRepository, LoginAccountRepository>();
        services.AddSingleton<ISearchService, SearchService>();
        services.AddSingleton<INotificationService, NotificationService>();
        services.AddSingleton<IPdfReportService, PdfReportService>();
        services.AddSingleton<IInvoicePdfService, InvoicePdfService>();
        services.AddSingleton<IMarketingPdfService, MarketingPdfService>();
        services.AddSingleton<IRentalOperationsService, RentalOperationsService>();
        services.AddSingleton<IAuthService, AuthService>();
        services.AddSingleton<ISessionService, SessionService>();
        services.AddSingleton<IPlatformSession, PlatformSession>();
        services.AddSingleton<HttpClient>();
        services.AddSingleton<IMachineIdService, MachineIdService>();
        services.AddSingleton<ILicenseClientService, LicenseClientService>();
        services.AddSingleton<IAdminSession, AdminSession>();
        services.AddSingleton<ILicenseApiClient, LicenseApiClient>();
        services.AddSingleton<IClientAccessMonitor, ClientAccessMonitor>();

        services.AddSingleton<INavigationService, NavigationService>();
        services.AddTransient<MainViewModel>();
        services.AddTransient<LoginViewModel>();
        services.AddTransient<ActivationWindow>();
        services.AddTransient<DashboardViewModel>();
        services.AddTransient<BookingsViewModel>();
        services.AddTransient<InventoryViewModel>();
        services.AddTransient<DispatchViewModel>();
        services.AddTransient<ReturnsViewModel>();
        services.AddTransient<BillingViewModel>();
        services.AddTransient<PaymentsViewModel>();
        services.AddTransient<ExpensesViewModel>();
        services.AddTransient<MaintenanceViewModel>();
        services.AddTransient<CustomersViewModel>();
        services.AddTransient<ReportsViewModel>();
        services.AddTransient<SettingsViewModel>();
        services.AddTransient<UsersViewModel>();
        services.AddTransient<LoginWindow>();
        services.AddTransient<MainWindow>();

        // Super Admin shell (hosted in same app)
        services.AddTransient<RentalX.Admin.ViewModels.MainViewModel>();
        services.AddTransient<RentalX.Admin.ViewModels.DashboardViewModel>();
        services.AddTransient<RentalX.Admin.ViewModels.ClientsViewModel>();
        services.AddTransient<RentalX.Admin.ViewModels.LicensesViewModel>();
        services.AddTransient<RentalX.Admin.ViewModels.PaymentsViewModel>();
        services.AddTransient<RentalX.Admin.ViewModels.SerialsViewModel>();
        services.AddTransient<RentalX.Admin.ViewModels.AccountViewModel>();
        services.AddTransient<RentalX.Admin.Views.MainWindow>();

        _services = services.BuildServiceProvider();
        ClientServiceHelper.Initialize(_services);
        AdminServiceHelper.Initialize(_services);

        try
        {
            using var scope = _services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            DatabaseInitializer.Initialize(db);
            scope.ServiceProvider.GetRequiredService<IAuthService>()
                .EnsureDefaultAccountAsync().GetAwaiter().GetResult();
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Database setup failed:\n{ex.Message}\n\nThe app will try to recreate the local database.",
                "RentalX Database",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);

            try
            {
                if (File.Exists(dbPath))
                {
                    File.Delete(dbPath);
                }

                using var scope = _services.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                DatabaseInitializer.Initialize(db);
                scope.ServiceProvider.GetRequiredService<IAuthService>()
                    .EnsureDefaultAccountAsync().GetAwaiter().GetResult();
            }
            catch (Exception recreateEx)
            {
                MessageBox.Show(
                    $"Could not create database:\n{recreateEx.Message}",
                    "RentalX Database Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                Shutdown();
                return;
            }
        }

        if (e.Args.Any(a => string.Equals(a, "--marketing-pdf", StringComparison.OrdinalIgnoreCase)))
        {
            try
            {
                var settings = _services.GetRequiredService<ISettingsRepository>();
                var marketing = _services.GetRequiredService<IMarketingPdfService>();
                var company = settings.GetAsync().GetAwaiter().GetResult();
                var path = marketing.Generate(company);
                var marker = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                    "RentalX",
                    "Marketing",
                    "last-generated.txt");
                File.WriteAllText(marker, path);
            }
            catch (Exception ex)
            {
                var marker = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                    "RentalX",
                    "Marketing",
                    "last-error.txt");
                Directory.CreateDirectory(Path.GetDirectoryName(marker)!);
                File.WriteAllText(marker, ex.ToString());
            }

            Shutdown();
            return;
        }

        if (e.Args.Any(a => string.Equals(a, "--delivery-guide", StringComparison.OrdinalIgnoreCase)))
        {
            try
            {
                var guide = new ProductDeliveryGuidePdfService();
                var path = guide.Generate();
                var marker = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                    "RentalX",
                    "Guides",
                    "last-delivery-guide.txt");
                Directory.CreateDirectory(Path.GetDirectoryName(marker)!);
                File.WriteAllText(marker, path);
            }
            catch (Exception ex)
            {
                var marker = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                    "RentalX",
                    "Guides",
                    "last-delivery-guide-error.txt");
                Directory.CreateDirectory(Path.GetDirectoryName(marker)!);
                File.WriteAllText(marker, ex.ToString());
            }

            Shutdown();
            return;
        }

        if (e.Args.Any(a => string.Equals(a, "--brochure-pdf", StringComparison.OrdinalIgnoreCase)))
        {
            try
            {
                var settings = _services.GetRequiredService<ISettingsRepository>();
                var company = settings.GetAsync().GetAwaiter().GetResult();
                var path = new ProductInterfaceBrochurePdfService().Generate(company);
                var marker = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                    "RentalX",
                    "Brochure",
                    "last-brochure.txt");
                Directory.CreateDirectory(Path.GetDirectoryName(marker)!);
                File.WriteAllText(marker, path);
            }
            catch (Exception ex)
            {
                var marker = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                    "RentalX",
                    "Brochure",
                    "last-brochure-error.txt");
                Directory.CreateDirectory(Path.GetDirectoryName(marker)!);
                File.WriteAllText(marker, ex.ToString());
            }

            Shutdown();
            return;
        }

        // License activation is handled by platform login + active subscription check on the API.
        var clientSession = _services.GetRequiredService<ISessionService>();
        clientSession.SignedOut += OnSignedOut;

        var adminSession = _services.GetRequiredService<IAdminSession>();
        adminSession.SignedOut += OnSignedOut;

        if (!ShowLogin())
        {
            Shutdown();
            return;
        }

        ShowShellForRole();
    }

    private bool ShowLogin()
    {
        _services!.GetRequiredService<IPlatformSession>().Clear();
        var login = _services.GetRequiredService<LoginWindow>();
        return login.ShowDialog() == true;
    }

    private void ShowShellForRole()
    {
        var platform = _services!.GetRequiredService<IPlatformSession>();
        if (platform.IsSuperAdmin)
        {
            ShowAdminShell();
            return;
        }

        if (platform.IsClient)
        {
            ShowClientShell();
            return;
        }

        MessageBox.Show("Unknown login role.", "RentalX", MessageBoxButton.OK, MessageBoxImage.Error);
        Shutdown();
    }

    private void ShowClientShell()
    {
        var window = _services!.GetRequiredService<MainWindow>();
        _activeShell = window;
        MainWindow = window;
        window.Closed += OnShellClosed;
        window.Show();
        _services.GetRequiredService<IClientAccessMonitor>().Start();
    }

    private void ShowAdminShell()
    {
        var window = _services!.GetRequiredService<RentalX.Admin.Views.MainWindow>();
        _activeShell = window;
        MainWindow = window;
        window.Closed += OnShellClosed;
        window.Show();
    }

    private void OnShellClosed(object? sender, EventArgs e)
    {
        if (_handlingSignOut)
        {
            return;
        }

        Shutdown();
    }

    private void OnSignedOut()
    {
        Dispatcher.Invoke(() =>
        {
            _handlingSignOut = true;
            try
            {
                if (_activeShell is not null)
                {
                    _activeShell.Closed -= OnShellClosed;
                    _activeShell.Close();
                    _activeShell = null;
                    MainWindow = null;
                }

                _services!.GetRequiredService<IClientAccessMonitor>().Stop();
                _services.GetRequiredService<IPlatformSession>().Clear();
                _services.GetRequiredService<IAdminSession>().Clear(raiseSignedOut: false);
                _services.GetRequiredService<ISessionService>().SignOut(raiseSignedOut: false);

                if (!ShowLogin())
                {
                    Shutdown();
                    return;
                }

                ShowShellForRole();
            }
            finally
            {
                _handlingSignOut = false;
            }
        });
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _services?.Dispose();
        base.OnExit(e);
    }
}
