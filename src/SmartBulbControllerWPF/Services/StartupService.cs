using Microsoft.Extensions.Logging;
using Microsoft.Win32;

namespace SmartBulbControllerWPF.Services;

public class StartupService : ServiceBase
{
    private const string RunKey = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";
    private const string AppName = "SmartBulbControllerWPF";

    public StartupService(ILogger<StartupService> logger) : base(logger) { }

    public void SetLaunchOnStartup(bool enable)
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunKey, writable: true);
            if (key is null) return;

            if (enable)
            {
                var exePath = Environment.ProcessPath ?? System.Reflection.Assembly.GetExecutingAssembly().Location;
                key.SetValue(AppName, $"\"{exePath}\"");
                Logger.LogInformation("Startup on login enabled");
            }
            else
            {
                key.DeleteValue(AppName, throwOnMissingValue: false);
                Logger.LogInformation("Startup on login disabled");
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to {Action} startup on login", enable ? "enable" : "disable");
        }
    }

    public bool IsLaunchOnStartupEnabled()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunKey);
            return key?.GetValue(AppName) is not null;
        }
        catch
        {
            return false;
        }
    }
}
