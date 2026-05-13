using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using H.NotifyIcon;
using MahApps.Metro.Controls;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using SmartBulbControllerWPF.Interfaces;
using SmartBulbControllerWPF.Services;
using SmartBulbControllerWPF.ViewModels;

namespace SmartBulbControllerWPF;

public partial class App : Application
{
    internal static bool IsExiting { get; private set; }

    private ServiceProvider? _serviceProvider;
    private TaskbarIcon?     _trayIcon;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var logPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "SmartBulbControllerWPF", "logs", "app-.log");

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .WriteTo.File(logPath, rollingInterval: RollingInterval.Day, retainedFileCountLimit: 14)
            .CreateLogger();

        var services = new ServiceCollection();
        services.AddLogging(b => b.AddSerilog(dispose: true));
        ConfigureServices(services);
        _serviceProvider = services.BuildServiceProvider();

        DispatcherUnhandledException                    += OnDispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException      += OnDomainUnhandledException;
        TaskScheduler.UnobservedTaskException           += OnUnobservedTaskException;

        Log.Information("SmartBulbControllerWPF starting");

        _serviceProvider.GetRequiredService<IThemeService>().ApplySaved();

        var mainWindow = _serviceProvider.GetRequiredService<MainWindow>();
        mainWindow.Show();

        SetupTrayIcon(mainWindow);
    }

    private void SetupTrayIcon(MainWindow mainWindow)
    {
        _trayIcon = new TaskbarIcon
        {
            IconSource  = CreateBulbIcon(),
            ToolTipText = "Smart Bulb Controller",
        };

        var menu = new System.Windows.Controls.ContextMenu();

        var openItem = new System.Windows.Controls.MenuItem { Header = "Open" };
        openItem.Click += (_, _) => ShowMainWindow(mainWindow);
        menu.Items.Add(openItem);

        menu.Items.Add(new System.Windows.Controls.Separator());

        var exitItem = new System.Windows.Controls.MenuItem { Header = "Exit" };
        exitItem.Click += (_, _) => { IsExiting = true; Shutdown(); };
        menu.Items.Add(exitItem);

        _trayIcon.ContextMenu      = menu;
        _trayIcon.TrayLeftMouseDoubleClick += (_, _) => ShowMainWindow(mainWindow);
    }

    private static void ShowMainWindow(MainWindow w)
    {
        w.Show();
        w.Activate();
        if (w.WindowState == WindowState.Minimized)
            w.WindowState = WindowState.Normal;
    }

    private static ImageSource CreateBulbIcon()
    {
        var bulb = new GeometryDrawing(
            new SolidColorBrush(Color.FromRgb(255, 210, 0)),
            new Pen(new SolidColorBrush(Color.FromRgb(180, 140, 0)), 0.5),
            new EllipseGeometry(new Point(8, 7), 5.5, 5.5));

        var baseRect = new GeometryDrawing(
            new SolidColorBrush(Color.FromRgb(160, 160, 160)),
            null,
            new RectangleGeometry(new Rect(5.5, 12.5, 5, 2)));

        var group = new DrawingGroup();
        group.Children.Add(bulb);
        group.Children.Add(baseRect);
        return new DrawingImage(group);
    }

    private static void ConfigureServices(IServiceCollection services)
    {
        services.AddHttpClient();
        services.AddSingleton<ISettingsService, SettingsService>();
        services.AddSingleton<IDeviceService, DeviceService>();
        services.AddSingleton<IDialogService, DialogService>();
        services.AddSingleton<IPresetService, PresetService>();
        services.AddSingleton<IThemeService, ThemeService>();
        services.AddSingleton<EspnScheduleService>();
        services.AddSingleton<IAlertService, AlertService>();
        services.AddSingleton<StartupService>();
        services.AddSingleton<MainWindow>();
        services.AddSingleton<MainViewModel>();
    }

    private void OnDispatcherUnhandledException(object sender,
        System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
    {
        Log.Fatal(e.Exception, "Unhandled UI exception");
        MessageBox.Show(
            $"An unexpected error occurred:\n{e.Exception.Message}",
            "Smart Bulb Controller",
            MessageBoxButton.OK,
            MessageBoxImage.Error);
        e.Handled = true;
    }

    private static void OnDomainUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        var ex = e.ExceptionObject as Exception;
        Log.Fatal(ex, "Unhandled domain exception (terminating={IsTerminating})", e.IsTerminating);
        Log.CloseAndFlush();
    }

    private static void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        Log.Error(e.Exception, "Unobserved task exception");
        e.SetObserved();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _trayIcon?.Dispose();
        Log.Information("SmartBulbControllerWPF exiting");
        Log.CloseAndFlush();
        _serviceProvider?.Dispose();
        base.OnExit(e);
    }
}
