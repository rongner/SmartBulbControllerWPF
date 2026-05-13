using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using SmartBulbControllerWPF.Interfaces;
using SmartBulbControllerWPF.Models;

namespace SmartBulbControllerWPF.Services;

public class SettingsService : ServiceBase, ISettingsService
{
    private static string DefaultPath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "SmartBulbControllerWPF", "settings.json");

    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    private readonly string _settingsPath;

    public AppSettings Current { get; private set; }

    public SettingsService(ILogger<SettingsService> logger)
        : this(logger, DefaultPath) { }

    internal SettingsService(ILogger<SettingsService> logger, string settingsPath) : base(logger)
    {
        _settingsPath = settingsPath;
        Current = Load();
    }

    public void Save()
    {
        ExecuteAsync(() =>
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_settingsPath)!);
            var json = JsonSerializer.Serialize(Current, JsonOptions);
            File.WriteAllText(_settingsPath, json);
            Logger.LogInformation("Settings saved");
            return Task.CompletedTask;
        }, "SaveSettings").GetAwaiter().GetResult();
    }

    public string? GetLocalKey()
    {
        if (string.IsNullOrEmpty(Current.EncryptedLocalKey)) return null;
        try
        {
            var encrypted = Convert.FromBase64String(Current.EncryptedLocalKey);
            var decrypted = ProtectedData.Unprotect(encrypted, null, DataProtectionScope.CurrentUser);
            return Encoding.UTF8.GetString(decrypted);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to decrypt local key");
            return null;
        }
    }

    public void SetLocalKey(string localKey)
    {
        var bytes = Encoding.UTF8.GetBytes(localKey);
        var encrypted = ProtectedData.Protect(bytes, null, DataProtectionScope.CurrentUser);
        Current.EncryptedLocalKey = Convert.ToBase64String(encrypted);
    }

    private AppSettings Load()
    {
        if (!File.Exists(_settingsPath)) return new AppSettings();
        try
        {
            var json = File.ReadAllText(_settingsPath);
            return JsonSerializer.Deserialize<AppSettings>(json, JsonOptions) ?? new AppSettings();
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Failed to load settings, using defaults");
            return new AppSettings();
        }
    }
}
