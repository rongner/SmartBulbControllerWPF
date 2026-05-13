using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using SmartBulbControllerWPF.Helpers;
using SmartBulbControllerWPF.Interfaces;
using SmartBulbControllerWPF.Models;
using SmartBulbControllerWPF.Services;

namespace SmartBulbControllerWPF.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    private readonly IDeviceService    _deviceService;
    private readonly ISettingsService  _settingsService;
    private readonly IDialogService    _dialogService;
    private readonly IPresetService    _presetService;
    private readonly IThemeService     _themeService;
    private readonly IAlertService     _alertService;
    private readonly ISceneService     _sceneService;
    private readonly StartupService    _startupService;

    private readonly Debouncer _brightDebounce    = new(250);
    private readonly Debouncer _colorTempDebounce = new(250);
    private readonly Debouncer _colorDebounce     = new(250);

    // Suppresses device calls during batch state loads and prevents RGB↔Hex↔Wheel feedback loops
    private bool _suppressChanges;

    // ── App version ───────────────────────────────────────────────────────────

    public string AppVersion =>
        System.Reflection.Assembly.GetEntryAssembly()
            ?.GetName().Version?.ToString(3) ?? "1.0.0";

    // ── Startup on login ─────────────────────────────────────────────────────

    [ObservableProperty]
    private bool _launchOnStartup;

    partial void OnLaunchOnStartupChanged(bool value)
    {
        if (_suppressChanges) return;
        _startupService.SetLaunchOnStartup(value);
        _settingsService.Current.LaunchOnStartup = value;
        _settingsService.Save();
    }

    // ── NBA alert settings ────────────────────────────────────────────────────

    public IReadOnlyList<NbaTeam> NbaTeams => Models.NbaTeams.All;

    [ObservableProperty]
    private NbaTeam? _selectedNbaTeam;

    [ObservableProperty]
    private string _alertTeamColorHex = "#888888";

    [ObservableProperty]
    private string _nextGameText = "–";

    [ObservableProperty]
    private bool _alertEnabled;

    [ObservableProperty]
    private int _alertLeadMinutes = 5;

    [ObservableProperty]
    private int _alertRevertHours = 3;

    [ObservableProperty]
    private int _alertBrightness = 100;

    partial void OnAlertEnabledChanged(bool value)    { if (!_suppressChanges) SaveAlertSettings(); }
    partial void OnAlertLeadMinutesChanged(int value) { if (!_suppressChanges) SaveAlertSettings(); }
    partial void OnAlertRevertHoursChanged(int value) { if (!_suppressChanges) SaveAlertSettings(); }
    partial void OnAlertBrightnessChanged(int value)  { if (!_suppressChanges) SaveAlertSettings(); }

    partial void OnSelectedNbaTeamChanged(NbaTeam? value)
    {
        if (_suppressChanges) return;
        AlertTeamColorHex = value is not null
            ? $"#{value.R:X2}{value.G:X2}{value.B:X2}"
            : "#888888";
        _settingsService.Current.NbaTeamId = value?.Id;
        _settingsService.Save();
    }

    private void SaveAlertSettings()
    {
        var cfg = _settingsService.Current;
        cfg.AlertEnabled     = AlertEnabled;
        cfg.LeadTimeMinutes  = AlertLeadMinutes;
        cfg.RevertAfterHours = AlertRevertHours;
        cfg.AlertBrightness  = AlertBrightness;
        _settingsService.Save();

        if (AlertEnabled) _alertService.Start();
        else              _alertService.Stop();
    }

    // ── Presets ───────────────────────────────────────────────────────────────

    [ObservableProperty]
    private ObservableCollection<Preset> _presets = [];

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ApplyPresetCommand))]
    [NotifyCanExecuteChangedFor(nameof(DeletePresetCommand))]
    private Preset? _selectedPreset;

    // ── Device list ───────────────────────────────────────────────────────────

    [ObservableProperty]
    private ObservableCollection<DiscoveredDevice> _discoveredDevices = [];

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ConnectCommand))]
    [NotifyCanExecuteChangedFor(nameof(RenameDeviceCommand))]
    private DiscoveredDevice? _selectedDevice;

    // ── Connection ────────────────────────────────────────────────────────────

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(DisconnectCommand))]
    [NotifyCanExecuteChangedFor(nameof(SetSceneCommand))]
    private bool _isConnected;

    [ObservableProperty]
    private string _statusText = "Not connected";

    // ── Power ─────────────────────────────────────────────────────────────────

    [ObservableProperty]
    private bool _isOn;

    // ── Light controls ────────────────────────────────────────────────────────

    [ObservableProperty]
    private int _brightness = 100;

    [ObservableProperty]
    private int _colorTemperature = 50;

    [ObservableProperty]
    private bool _isWhiteMode = true;

    [ObservableProperty]
    private byte _colorRed = 255;

    [ObservableProperty]
    private byte _colorGreen = 255;

    [ObservableProperty]
    private byte _colorBlue = 255;

    [ObservableProperty]
    private string _hexColor = "#FFFFFF";

    // ── Color wheel (HSV hue + saturation) ───────────────────────────────────

    [ObservableProperty]
    private double _colorHue;

    [ObservableProperty]
    private double _colorSaturation;

    partial void OnColorHueChanged(double value)        => OnWheelChanged();
    partial void OnColorSaturationChanged(double value) => OnWheelChanged();

    private void OnWheelChanged()
    {
        if (_suppressChanges) return;
        var (r, g, b) = ColorHelper.HsvToRgb(ColorHue, ColorSaturation, 1.0);
        _suppressChanges = true;
        ColorRed         = r;
        ColorGreen       = g;
        ColorBlue        = b;
        HexColor         = $"#{r:X2}{g:X2}{b:X2}";
        _suppressChanges = false;

        if (!IsConnected) return;
        _colorDebounce.Schedule(() => _deviceService.SetColorAsync(r, g, b));
    }

    // ── Animated scenes ───────────────────────────────────────────────────────

    [ObservableProperty]
    private SceneType _activeScene = SceneType.None;

    [ObservableProperty]
    private int _sceneStepMs = 80;

    public bool IsColorCycleActive => ActiveScene == SceneType.ColorCycle;
    public bool IsPulseActive      => ActiveScene == SceneType.Pulse;
    public bool IsStrobeActive     => ActiveScene == SceneType.Strobe;

    partial void OnActiveSceneChanged(SceneType value)
    {
        OnPropertyChanged(nameof(IsColorCycleActive));
        OnPropertyChanged(nameof(IsPulseActive));
        OnPropertyChanged(nameof(IsStrobeActive));
    }

    [RelayCommand(CanExecute = nameof(CanSetScene))]
    private void SetScene(SceneType scene)
    {
        if (scene == ActiveScene || scene == SceneType.None)
        {
            _sceneService.Stop();
            ActiveScene = SceneType.None;
            return;
        }
        _sceneService.Start(scene, ColorRed, ColorGreen, ColorBlue, SceneStepMs);
        ActiveScene = scene;
    }

    private bool CanSetScene() => IsConnected;

    // ── Constructor ───────────────────────────────────────────────────────────

    public MainViewModel(
        IDeviceService   deviceService,
        ISettingsService settingsService,
        IDialogService   dialogService,
        IPresetService   presetService,
        IThemeService    themeService,
        IAlertService    alertService,
        ISceneService    sceneService,
        StartupService   startupService,
        ILogger<MainViewModel> logger) : base(logger)
    {
        _deviceService   = deviceService;
        _settingsService = settingsService;
        _dialogService   = dialogService;
        _presetService   = presetService;
        _themeService    = themeService;
        _alertService    = alertService;
        _sceneService    = sceneService;
        _startupService  = startupService;

        foreach (var p in presetService.Presets)
            Presets.Add(p);

        var cfg = settingsService.Current;
        _suppressChanges = true;
        AlertEnabled     = cfg.AlertEnabled;
        AlertLeadMinutes = cfg.LeadTimeMinutes;
        AlertRevertHours = cfg.RevertAfterHours;
        AlertBrightness  = cfg.AlertBrightness;
        SelectedNbaTeam  = cfg.NbaTeamId.HasValue
            ? Models.NbaTeams.All.FirstOrDefault(t => t.Id == cfg.NbaTeamId.Value)
            : null;
        _suppressChanges = false;

        AlertTeamColorHex = SelectedNbaTeam is not null
            ? $"#{SelectedNbaTeam.R:X2}{SelectedNbaTeam.G:X2}{SelectedNbaTeam.B:X2}"
            : "#888888";

        alertService.NextGameUpdated += OnNextGameUpdated;

        LaunchOnStartup = startupService.IsLaunchOnStartupEnabled();
        if (cfg.AlertEnabled) alertService.Start();
    }

    // ── Scan ──────────────────────────────────────────────────────────────────

    [RelayCommand]
    private async Task ScanAsync()
    {
        await RunBusyAsync(async () =>
        {
            StatusText = "Scanning…";
            DiscoveredDevices.Clear();
            var devices = await _deviceService.ScanAsync();
            foreach (var d in devices)
                DiscoveredDevices.Add(d);
            StatusText = DiscoveredDevices.Count > 0
                ? $"Found {DiscoveredDevices.Count} device(s)"
                : "No devices found";
        });
    }

    // ── Connect ───────────────────────────────────────────────────────────────

    [RelayCommand(CanExecute = nameof(CanConnect))]
    private async Task ConnectAsync()
    {
        var result = await _dialogService.ShowConnectDialogAsync(
            SelectedDevice?.Ip,
            SelectedDevice?.DeviceId);

        if (result is null) return;

        await RunBusyAsync(async () =>
        {
            await _deviceService.ConnectAsync(result.Ip, result.DeviceId, result.LocalKey);

            _settingsService.Current.DeviceId = result.DeviceId;
            _settingsService.Current.DeviceIp = result.Ip;
            _settingsService.SetLocalKey(result.LocalKey);
            _settingsService.Save();

            var state = await _deviceService.GetStateAsync();
            LoadStateQuiet(state);
            IsConnected = true;
            StatusText  = $"Connected — {result.Ip}";
        });
    }

    private bool CanConnect() => !IsConnected;

    // ── Disconnect ────────────────────────────────────────────────────────────

    [RelayCommand(CanExecute = nameof(CanDisconnect))]
    private void Disconnect()
    {
        _sceneService.Stop();
        ActiveScene = SceneType.None;
        _deviceService.Disconnect();
        IsConnected = false;
        LoadStateQuiet(new DeviceState());
        StatusText  = "Not connected";
    }

    private bool CanDisconnect() => IsConnected;

    // ── Power (driven by ToggleSwitch TwoWay binding) ─────────────────────────

    partial void OnIsOnChanged(bool value)
    {
        if (_suppressChanges || !IsConnected) return;
        _ = RunBusyAsync(() => _deviceService.SetPowerAsync(value));
    }

    // ── Brightness ────────────────────────────────────────────────────────────

    partial void OnBrightnessChanged(int value)
    {
        if (_suppressChanges || !IsConnected) return;
        _brightDebounce.Schedule(() => _deviceService.SetBrightnessAsync(value));
    }

    // ── Color temperature ─────────────────────────────────────────────────────

    partial void OnColorTemperatureChanged(int value)
    {
        if (_suppressChanges || !IsConnected) return;
        _colorTempDebounce.Schedule(() => _deviceService.SetColorTemperatureAsync(value));
    }

    // ── Mode toggle ───────────────────────────────────────────────────────────

    partial void OnIsWhiteModeChanged(bool value)
    {
        if (_suppressChanges || !IsConnected) return;
        _ = RunBusyAsync(async () =>
        {
            if (value)
                await _deviceService.SetColorTemperatureAsync(ColorTemperature);
            else
                await _deviceService.SetColorAsync(ColorRed, ColorGreen, ColorBlue);
        });
    }

    // ── RGB / Hex ─────────────────────────────────────────────────────────────

    partial void OnColorRedChanged(byte value)   => ScheduleColorSend(fromHex: false);
    partial void OnColorGreenChanged(byte value) => ScheduleColorSend(fromHex: false);
    partial void OnColorBlueChanged(byte value)  => ScheduleColorSend(fromHex: false);

    partial void OnHexColorChanged(string value)
    {
        if (_suppressChanges) return;
        if (!TryParseHex(value, out var r, out var g, out var b)) return;

        _suppressChanges = true;
        ColorRed   = r;
        ColorGreen = g;
        ColorBlue  = b;
        _suppressChanges = false;

        ScheduleColorSend(fromHex: true);
    }

    private void ScheduleColorSend(bool fromHex)
    {
        if (_suppressChanges) return;

        _suppressChanges = true;
        if (!fromHex)
            HexColor = $"#{ColorRed:X2}{ColorGreen:X2}{ColorBlue:X2}";
        var (h, s, _) = ColorHelper.RgbToHsv(ColorRed, ColorGreen, ColorBlue);
        ColorHue        = h;
        ColorSaturation = s;
        _suppressChanges = false;

        if (!IsConnected) return;
        var (r, g, b) = (ColorRed, ColorGreen, ColorBlue);
        _colorDebounce.Schedule(() => _deviceService.SetColorAsync(r, g, b));
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private void LoadStateQuiet(DeviceState state)
    {
        _suppressChanges = true;
        IsOn             = state.Power;
        Brightness       = state.Brightness > 0 ? state.Brightness : 100;
        ColorTemperature = state.ColorTemperature;
        ColorRed         = state.Color.R;
        ColorGreen       = state.Color.G;
        ColorBlue        = state.Color.B;
        HexColor         = $"#{state.Color.R:X2}{state.Color.G:X2}{state.Color.B:X2}";
        var (h, s, _) = ColorHelper.RgbToHsv(state.Color.R, state.Color.G, state.Color.B);
        ColorHue        = h;
        ColorSaturation = s;
        IsWhiteMode      = state.Mode != "colour";
        _suppressChanges = false;
    }

    internal static bool TryParseHex(string? value, out byte r, out byte g, out byte b)
    {
        r = g = b = 0;
        if (string.IsNullOrEmpty(value)) return false;
        var s = value.TrimStart('#');
        if (s.Length != 6) return false;
        if (!int.TryParse(s, NumberStyles.HexNumber, null, out var rgb)) return false;
        r = (byte)((rgb >> 16) & 0xFF);
        g = (byte)((rgb >>  8) & 0xFF);
        b = (byte)(rgb & 0xFF);
        return true;
    }

    // ── Presets ───────────────────────────────────────────────────────────────

    [RelayCommand(CanExecute = nameof(CanApplyPreset))]
    private async Task ApplyPresetAsync()
    {
        var preset = SelectedPreset!;
        await RunBusyAsync(async () =>
        {
            _suppressChanges = true;
            IsWhiteMode      = preset.IsWhiteMode;
            Brightness       = preset.Brightness;
            ColorTemperature = preset.ColorTemperature;
            ColorRed         = preset.R;
            ColorGreen       = preset.G;
            ColorBlue        = preset.B;
            HexColor         = $"#{preset.R:X2}{preset.G:X2}{preset.B:X2}";
            var (h, s, _) = ColorHelper.RgbToHsv(preset.R, preset.G, preset.B);
            ColorHue        = h;
            ColorSaturation = s;
            _suppressChanges = false;

            if (preset.IsWhiteMode)
            {
                await _deviceService.SetBrightnessAsync(preset.Brightness);
                await _deviceService.SetColorTemperatureAsync(preset.ColorTemperature);
            }
            else
            {
                await _deviceService.SetColorAsync(preset.R, preset.G, preset.B);
                await _deviceService.SetBrightnessAsync(preset.Brightness);
            }
        });
    }

    private bool CanApplyPreset() => SelectedPreset is not null && IsConnected;

    [RelayCommand]
    private async Task SavePresetAsync()
    {
        var name = await _dialogService.ShowInputAsync("Save Preset", "Enter a name for this preset:");
        if (string.IsNullOrWhiteSpace(name)) return;

        var preset = new Preset
        {
            Name             = name.Trim(),
            IsWhiteMode      = IsWhiteMode,
            Brightness       = Brightness,
            ColorTemperature = ColorTemperature,
            R                = ColorRed,
            G                = ColorGreen,
            B                = ColorBlue,
        };

        _presetService.SaveCustom(preset);

        var existing = Presets.FirstOrDefault(p => !p.IsBuiltIn && p.Name == preset.Name);
        if (existing is not null)
            Presets[Presets.IndexOf(existing)] = preset;
        else
            Presets.Add(preset);
    }

    [RelayCommand(CanExecute = nameof(CanDeletePreset))]
    private async Task DeletePresetAsync()
    {
        var preset = SelectedPreset!;
        var confirmed = await _dialogService.ShowConfirmAsync(
            "Delete Preset", $"Delete '{preset.Name}'?");
        if (!confirmed) return;

        _presetService.Delete(preset);
        Presets.Remove(preset);
        SelectedPreset = null;
    }

    private bool CanDeletePreset() => SelectedPreset is { IsBuiltIn: false };

    // ── Auto-reconnect ────────────────────────────────────────────────────────

    public async Task AutoReconnectAsync()
    {
        var cfg = _settingsService.Current;
        if (string.IsNullOrEmpty(cfg.DeviceIp) || string.IsNullOrEmpty(cfg.DeviceId)) return;
        var localKey = _settingsService.GetLocalKey();
        if (string.IsNullOrEmpty(localKey)) return;

        if (!DiscoveredDevices.Any(d => d.Ip == cfg.DeviceIp))
        {
            DiscoveredDevices.Add(new DiscoveredDevice(cfg.DeviceIp, cfg.DeviceId, "3.3")
            {
                FriendlyName = _settingsService.GetFriendlyName(cfg.DeviceIp)
            });
        }

        StatusText = $"Reconnecting to {cfg.DeviceIp}…";
        await RunBusyAsync(async () =>
        {
            await _deviceService.ConnectAsync(cfg.DeviceIp, cfg.DeviceId, localKey);
            var state = await _deviceService.GetStateAsync();
            LoadStateQuiet(state);
            IsConnected = true;
            StatusText  = $"Connected — {cfg.DeviceIp}";
        });
    }

    // ── Rename device ─────────────────────────────────────────────────────────

    [RelayCommand(CanExecute = nameof(CanRenameDevice))]
    private async Task RenameDeviceAsync()
    {
        var device = SelectedDevice!;
        var name   = await _dialogService.ShowInputAsync("Rename Device", $"Enter a label for {device.Ip}:");
        if (string.IsNullOrWhiteSpace(name)) return;

        name = name.Trim();
        _settingsService.SetFriendlyName(device.Ip, name);
        _settingsService.Save();

        var updated = device with { FriendlyName = name };
        var idx     = DiscoveredDevices.IndexOf(device);
        if (idx >= 0) DiscoveredDevices[idx] = updated;
        SelectedDevice = updated;
    }

    private bool CanRenameDevice() => SelectedDevice is not null;

    // ── Clear saved device ────────────────────────────────────────────────────

    [RelayCommand]
    private void ClearSavedDevice()
    {
        _settingsService.Current.DeviceId           = null;
        _settingsService.Current.DeviceIp           = null;
        _settingsService.Current.EncryptedLocalKey  = null;
        _settingsService.Save();
        if (IsConnected) Disconnect();
    }

    // ── Next game update (called from background thread) ──────────────────────

    private void OnNextGameUpdated()
    {
        var t    = _alertService.NextGameTime;
        var text = t.HasValue ? $"Next: {t.Value:ddd MMM d, h:mm tt}" : "No upcoming games";
        var dispatcher = System.Windows.Application.Current?.Dispatcher;
        if (dispatcher is not null && !dispatcher.CheckAccess())
            dispatcher.Invoke(() => NextGameText = text);
        else
            NextGameText = text;
    }

    // ── Theme ─────────────────────────────────────────────────────────────────

    [RelayCommand]
    private void SetTheme(ThemePreference preference)
    {
        _settingsService.Current.Theme = preference;
        _settingsService.Save();
        _themeService.Apply(preference);
    }

    // ── Menu ──────────────────────────────────────────────────────────────────

    [RelayCommand]
    private async Task ShowAboutAsync() =>
        await _dialogService.ShowAboutAsync();

    [RelayCommand]
    private void Exit() => Application.Current.Shutdown();
}
