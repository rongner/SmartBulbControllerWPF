using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using SmartBulbControllerWPF.Interfaces;
using SmartBulbControllerWPF.Models;

namespace SmartBulbControllerWPF.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    private readonly IDeviceService    _deviceService;
    private readonly ISettingsService  _settingsService;
    private readonly IDialogService    _dialogService;

    // Prevents OnIsOnChanged from sending to device when setting state programmatically
    private bool _suppressPowerChange;

    [ObservableProperty]
    private ObservableCollection<DiscoveredDevice> _discoveredDevices = [];

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ConnectCommand))]
    private DiscoveredDevice? _selectedDevice;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(DisconnectCommand))]
    private bool _isConnected;

    [ObservableProperty]
    private bool _isOn;

    [ObservableProperty]
    private string _statusText = "Not connected";

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
            SetIsOnQuiet(state.Power);
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
        SetIsOnQuiet(false);
        StatusText  = "Not connected";
    }

    private bool CanDisconnect() => IsConnected;

    // ── Power toggle (driven by ToggleSwitch TwoWay binding) ──────────────────

    partial void OnIsOnChanged(bool value)
    {
        if (_suppressPowerChange || !IsConnected) return;
        _ = RunBusyAsync(() => _deviceService.SetPowerAsync(value));
    }

    private void SetIsOnQuiet(bool value)
    {
        _suppressPowerChange = true;
        IsOn = value;
        _suppressPowerChange = false;
    }

    // ── Menu ──────────────────────────────────────────────────────────────────

    [RelayCommand]
    private async Task ShowAboutAsync() =>
        await _dialogService.ShowAboutAsync();

    [RelayCommand]
    private void Exit() => Application.Current.Shutdown();
}
