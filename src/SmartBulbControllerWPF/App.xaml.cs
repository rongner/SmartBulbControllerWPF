using System.IO;
using System.Windows;
using MahApps.Metro.Controls;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using SmartBulbControllerWPF.Services;
using SmartBulbControllerWPF.ViewModels;

namespace SmartBulbControllerWPF;

public partial class App : Application
{
    private ServiceProvider? _serviceProvider;

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

        Log.Information("SmartBulbControllerWPF starting");

        var mainWindow = _serviceProvider.GetRequiredService<MainWindow>();
        mainWindow.Show();
    }

    private static void ConfigureServices(IServiceCollection services)
    {
        services.AddHttpClient();
        services.AddSingleton<MainWindow>();
        services.AddSingleton<MainViewModel>();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        Log.Information("SmartBulbControllerWPF exiting");
        Log.CloseAndFlush();
        _serviceProvider?.Dispose();
        base.OnExit(e);
    }
}
