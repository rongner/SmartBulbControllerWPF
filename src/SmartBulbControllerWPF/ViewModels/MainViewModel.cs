using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using SmartBulbControllerWPF.Helpers;
using SmartBulbControllerWPF.Interfaces;
using SmartBulbControllerWPF.Models;

namespace SmartBulbControllerWPF.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    private readonly IDeviceService    _deviceService;
    private readonly ISettingsService  _settingsService;
    private readonly IDialogService    _dialogService;

    private readonly Debouncer _brightDebounce    = new(250);
    private readonly Debouncer _colorTempDebounce = new(250);
    private readonly Debouncer _colorDebounce     = new(250);

    // Suppresses device calls during batch state loads and prevents RGB↔Hex feedback loops
    private bool _suppressChanges;

    // ── Device list ───────────────────────────────────────────────────────────

    [ObservableProperty]
    private ObservableCollection<DiscoveredDevice> _discoveredDevices = [];

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ConnectCommand))]
    private DiscoveredDevice? _selectedDevice;

    // ── Connection ────────────────────────────────────────────────────────────

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(DisconnectCommand))]
    private bool _isConnected;

    [ObservableProperty]
    private string _statusText = "Not connected";

    // ── Power ─────────────────────────────────────────────────────────────────

    [ObservableProperty]
    private bool _isOn;

    // ── Light controls ────────────────────────────────────────────────────────

    [ObservableProperty]
    private int _brightness = 100;        // 0–100 %

    [ObservableProperty]
    private int _colorTemperature = 50;   // 0–100 % (0 = warm, 100 = cool)

    [ObservableProperty]
    private bool _isWhiteMode = true;     // true = white/temp, false = colour

    [ObservableProperty]
    private byte _colorRed = 255;

    [ObservableProperty]
    private byte _colorGreen = 255;

    [ObservableProperty]
    private byte _colorBlue = 255;

    [ObservableProperty]
    private string _hexColor = "#FFFFFF";

    // ── Constructor ───────────────────────────────────────────────────────────

    public MainViewModel(
        IDeviceService   deviceService,
        ISettingsService settingsService,
        IDialogService   dialogService,
        ILogger<MainViewModel> logger) : base(logger)
    {
        _deviceService   = deviceService;
        _settingsService = settingsService;
        _dialogService   = dialogService;
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

        if (!fromHex)
        {
            _suppressChanges = true;
            HexColor = $"#{ColorRed:X2}{ColorGreen:X2}{ColorBlue:X2}";
            _suppressChanges = false;
        }

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

    // ── Menu ──────────────────────────────────────────────────────────────────

    [RelayCommand]
    private async Task ShowAboutAsync() =>
        await _dialogService.ShowAboutAsync();

    [RelayCommand]
    private void Exit() => Application.Current.Shutdown();
}
