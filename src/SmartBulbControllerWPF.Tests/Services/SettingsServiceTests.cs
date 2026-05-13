using System.IO;
using Microsoft.Extensions.Logging.Abstractions;
using SmartBulbControllerWPF.Models;
using SmartBulbControllerWPF.Services;

namespace SmartBulbControllerWPF.Tests.Services;

public class SettingsServiceTests : IDisposable
{
    private readonly string _settingsPath;

    public SettingsServiceTests()
    {
        _settingsPath = Path.Combine(Path.GetTempPath(), $"SBCTest_{Guid.NewGuid()}", "settings.json");
    }

    public void Dispose()
    {
        var dir = Path.GetDirectoryName(_settingsPath)!;
        if (Directory.Exists(dir))
            Directory.Delete(dir, recursive: true);
    }

    private SettingsService CreateService() =>
        new(NullLogger<SettingsService>.Instance, _settingsPath);

    [Fact]
    public void Defaults_are_applied_when_no_file_exists()
    {
        var svc = CreateService();

        Assert.Equal(ThemePreference.System, svc.Current.Theme);
        Assert.Equal(5, svc.Current.LeadTimeMinutes);
        Assert.Equal(3, svc.Current.RevertAfterHours);
        Assert.False(svc.Current.AlertEnabled);
        Assert.False(svc.Current.LaunchOnStartup);
    }

    [Fact]
    public void Save_and_reload_round_trips_settings()
    {
        var svc = CreateService();
        svc.Current.Theme = ThemePreference.Dark;
        svc.Current.LeadTimeMinutes = 10;
        svc.Current.NbaTeamId = 2;
        svc.Save();

        var svc2 = CreateService();
        Assert.Equal(ThemePreference.Dark, svc2.Current.Theme);
        Assert.Equal(10, svc2.Current.LeadTimeMinutes);
        Assert.Equal(2, svc2.Current.NbaTeamId);
    }

    [Fact]
    public void SetLocalKey_encrypts_and_GetLocalKey_decrypts()
    {
        var svc = CreateService();
        const string key = "super-secret-local-key";

        svc.SetLocalKey(key);

        Assert.NotNull(svc.Current.EncryptedLocalKey);
        Assert.NotEqual(key, svc.Current.EncryptedLocalKey);
        Assert.Equal(key, svc.GetLocalKey());
    }

    [Fact]
    public void GetLocalKey_returns_null_when_no_key_set()
    {
        var svc = CreateService();
        Assert.Null(svc.GetLocalKey());
    }

    [Fact]
    public void LocalKey_survives_save_and_reload()
    {
        var svc = CreateService();
        svc.SetLocalKey("my-key");
        svc.Save();

        var svc2 = CreateService();
        Assert.Equal("my-key", svc2.GetLocalKey());
    }
}
