namespace SmartBulbControllerWPF.Models;

public class AppSettings
{
    // Device
    public string? DeviceId      { get; set; }
    public string? DeviceIp      { get; set; }
    public string? EncryptedLocalKey { get; set; }  // DPAPI-encrypted, Base64

    // Team Game Alerts
    public int?   NbaTeamId         { get; set; }
    public bool   AlertEnabled      { get; set; } = false;
    public int    LeadTimeMinutes   { get; set; } = 5;
    public int    RevertAfterHours  { get; set; } = 3;
    public int    AlertBrightness   { get; set; } = 100;

    // Theme
    public ThemePreference Theme { get; set; } = ThemePreference.System;

    // Startup
    public bool LaunchOnStartup { get; set; } = false;
}

public enum ThemePreference { System, Light, Dark }
