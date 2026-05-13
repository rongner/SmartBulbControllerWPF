using ControlzEx.Theming;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using SmartBulbControllerWPF.Interfaces;
using SmartBulbControllerWPF.Models;

namespace SmartBulbControllerWPF.Services;

public class ThemeService : ServiceBase, IThemeService
{
    private readonly ISettingsService _settings;
    private const string LightTheme = "Light.Blue";
    private const string DarkTheme  = "Dark.Blue";

    public ThemeService(ISettingsService settings, ILogger<ThemeService> logger) : base(logger)
    {
        _settings = settings;
    }

    public void Apply(ThemePreference preference)
    {
        var themeName = preference switch
        {
            ThemePreference.Light  => LightTheme,
            ThemePreference.Dark   => DarkTheme,
            _                      => IsSystemDark() ? DarkTheme : LightTheme,
        };

        ThemeManager.Current.ChangeTheme(System.Windows.Application.Current, themeName);
        Logger.LogInformation("Applied theme {Theme}", themeName);
    }

    public void ApplySaved() => Apply(_settings.Current.Theme);

    internal static bool IsSystemDark()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(
                @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
            var value = key?.GetValue("AppsUseLightTheme");
            return value is int i && i == 0;
        }
        catch
        {
            return false;
        }
    }
}
