using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using SmartBulbControllerWPF.Interfaces;
using SmartBulbControllerWPF.Models;
using SmartBulbControllerWPF.Services;
using SmartBulbControllerWPF.ViewModels;

namespace SmartBulbControllerWPF.Tests.ViewModels;

public class CommandIntegrationTests
{
    private readonly Mock<IDeviceService>   _deviceSvc   = new();
    private readonly Mock<ISettingsService> _settingsSvc = new();
    private readonly Mock<IDialogService>   _dialogSvc   = new();
    private readonly Mock<IPresetService>   _presetSvc   = new();
    private readonly Mock<IAlertService>    _alertSvc    = new();
    private AppSettings _appSettings = new();

    private MainViewModel CreateVm()
    {
        _settingsSvc.SetupGet(s => s.Current).Returns(_appSettings);
        _presetSvc.SetupGet(s => s.Presets).Returns([]);
        return new MainViewModel(
            _deviceSvc.Object,
            _settingsSvc.Object,
            _dialogSvc.Object,
            _presetSvc.Object,
            new Mock<IThemeService>().Object,
            _alertSvc.Object,
            new StartupService(NullLogger<StartupService>.Instance),
            NullLogger<MainViewModel>.Instance);
    }

    // ── AutoReconnect ─────────────────────────────────────────────────────────

    [Fact]
    public async Task AutoReconnect_NoSavedIp_DoesNotAttemptConnect()
    {
        var vm = CreateVm(); // AppSettings.DeviceIp is null

        await vm.AutoReconnectAsync();

        _deviceSvc.Verify(s => s.ConnectAsync(
            It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task AutoReconnect_NoLocalKey_DoesNotAttemptConnect()
    {
        _appSettings = new AppSettings { DeviceIp = "10.0.0.5", DeviceId = "dev01" };
        // GetLocalKey already returns null via setup

        var vm = CreateVm();
        await vm.AutoReconnectAsync();

        _deviceSvc.Verify(s => s.ConnectAsync(
            It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task AutoReconnect_WithStoredCredentials_ConnectsWithoutDialog()
    {
        _appSettings = new AppSettings { DeviceIp = "10.0.0.5", DeviceId = "dev01" };
        _settingsSvc.Setup(s => s.GetLocalKey()).Returns("mykey");
        _deviceSvc.Setup(s => s.ConnectAsync("10.0.0.5", "dev01", "mykey", It.IsAny<CancellationToken>()))
                  .Returns(Task.CompletedTask);
        _deviceSvc.Setup(s => s.GetStateAsync(It.IsAny<CancellationToken>()))
                  .ReturnsAsync(new DeviceState { Power = true });

        var vm = CreateVm();
        await vm.AutoReconnectAsync();

        Assert.True(vm.IsConnected);
        _dialogSvc.Verify(d => d.ShowConnectDialogAsync(
            It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task AutoReconnect_DeviceOffline_SetsErrorAndNotConnected()
    {
        _appSettings = new AppSettings { DeviceIp = "10.0.0.5", DeviceId = "dev01" };
        _settingsSvc.Setup(s => s.GetLocalKey()).Returns("mykey");
        _deviceSvc.Setup(s => s.ConnectAsync(It.IsAny<string>(), It.IsAny<string>(),
                             It.IsAny<string>(), It.IsAny<CancellationToken>()))
                  .ThrowsAsync(new Exception("device unreachable"));

        var vm = CreateVm();
        await vm.AutoReconnectAsync();

        Assert.False(vm.IsConnected);
        Assert.Equal("device unreachable", vm.ErrorMessage);
    }

    [Fact]
    public async Task AutoReconnect_PopulatesDeviceListFromSettings()
    {
        _appSettings = new AppSettings { DeviceIp = "10.0.0.5", DeviceId = "dev01" };
        _settingsSvc.Setup(s => s.GetLocalKey()).Returns("mykey");
        _deviceSvc.Setup(s => s.ConnectAsync(It.IsAny<string>(), It.IsAny<string>(),
                             It.IsAny<string>(), It.IsAny<CancellationToken>()))
                  .ThrowsAsync(new Exception("offline"));

        var vm = CreateVm();
        await vm.AutoReconnectAsync();

        Assert.Single(vm.DiscoveredDevices);
        Assert.Equal("10.0.0.5", vm.DiscoveredDevices[0].Ip);
    }

    // ── ApplyPreset ───────────────────────────────────────────────────────────

    [Fact]
    public async Task ApplyPreset_WhiteMode_CallsBrightnessAndColorTemp()
    {
        var result = new ConnectDialogResult("10.0.0.5", "dev01", "key");
        _dialogSvc.Setup(d => d.ShowConnectDialogAsync(It.IsAny<string>(), It.IsAny<string>()))
                  .ReturnsAsync(result);
        _deviceSvc.Setup(s => s.ConnectAsync(It.IsAny<string>(), It.IsAny<string>(),
                             It.IsAny<string>(), It.IsAny<CancellationToken>()))
                  .Returns(Task.CompletedTask);
        _deviceSvc.Setup(s => s.GetStateAsync(It.IsAny<CancellationToken>()))
                  .ReturnsAsync(new DeviceState());
        _deviceSvc.Setup(s => s.SetBrightnessAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                  .Returns(Task.CompletedTask);
        _deviceSvc.Setup(s => s.SetColorTemperatureAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                  .Returns(Task.CompletedTask);

        var vm = CreateVm();
        await ((IAsyncRelayCommand)vm.ConnectCommand).ExecuteAsync(null);

        var preset = new Preset { Name = "Warm", IsWhiteMode = true, Brightness = 75, ColorTemperature = 20 };
        vm.SelectedPreset = preset;
        await ((IAsyncRelayCommand)vm.ApplyPresetCommand).ExecuteAsync(null);

        _deviceSvc.Verify(s => s.SetBrightnessAsync(75, It.IsAny<CancellationToken>()), Times.Once);
        _deviceSvc.Verify(s => s.SetColorTemperatureAsync(20, It.IsAny<CancellationToken>()), Times.Once);
        _deviceSvc.Verify(s => s.SetColorAsync(It.IsAny<byte>(), It.IsAny<byte>(),
            It.IsAny<byte>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ApplyPreset_ColorMode_CallsSetColor()
    {
        var result = new ConnectDialogResult("10.0.0.5", "dev01", "key");
        _dialogSvc.Setup(d => d.ShowConnectDialogAsync(It.IsAny<string>(), It.IsAny<string>()))
                  .ReturnsAsync(result);
        _deviceSvc.Setup(s => s.ConnectAsync(It.IsAny<string>(), It.IsAny<string>(),
                             It.IsAny<string>(), It.IsAny<CancellationToken>()))
                  .Returns(Task.CompletedTask);
        _deviceSvc.Setup(s => s.GetStateAsync(It.IsAny<CancellationToken>()))
                  .ReturnsAsync(new DeviceState());
        _deviceSvc.Setup(s => s.SetColorAsync(It.IsAny<byte>(), It.IsAny<byte>(),
                             It.IsAny<byte>(), It.IsAny<CancellationToken>()))
                  .Returns(Task.CompletedTask);
        _deviceSvc.Setup(s => s.SetBrightnessAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                  .Returns(Task.CompletedTask);

        var vm = CreateVm();
        await ((IAsyncRelayCommand)vm.ConnectCommand).ExecuteAsync(null);

        var preset = new Preset { Name = "Teal", IsWhiteMode = false, R = 0, G = 128, B = 128, Brightness = 90 };
        vm.SelectedPreset = preset;
        await ((IAsyncRelayCommand)vm.ApplyPresetCommand).ExecuteAsync(null);

        _deviceSvc.Verify(s => s.SetColorAsync(0, 128, 128, It.IsAny<CancellationToken>()), Times.Once);
        _deviceSvc.Verify(s => s.SetBrightnessAsync(90, It.IsAny<CancellationToken>()), Times.Once);
    }

    // ── RenameDevice ──────────────────────────────────────────────────────────

    [Fact]
    public async Task RenameDevice_UpdatesDisplayNameInListAndSavesSettings()
    {
        _dialogSvc.Setup(d => d.ShowInputAsync("Rename Device", It.IsAny<string>()))
                  .ReturnsAsync("Living Room");

        var vm = CreateVm();
        var device = new DiscoveredDevice("10.0.0.5", "dev01", "3.3");
        vm.DiscoveredDevices.Add(device);
        vm.SelectedDevice = device;

        await ((IAsyncRelayCommand)vm.RenameDeviceCommand).ExecuteAsync(null);

        Assert.Equal("Living Room", vm.DiscoveredDevices[0].FriendlyName);
        Assert.Equal("Living Room", vm.DiscoveredDevices[0].DisplayName);
        _settingsSvc.Verify(s => s.SetFriendlyName("10.0.0.5", "Living Room"), Times.Once);
        _settingsSvc.Verify(s => s.Save(), Times.Once);
    }

    [Fact]
    public async Task RenameDevice_DialogCancelled_DoesNotUpdate()
    {
        _dialogSvc.Setup(d => d.ShowInputAsync(It.IsAny<string>(), It.IsAny<string>()))
                  .ReturnsAsync((string?)null);

        var vm     = CreateVm();
        var device = new DiscoveredDevice("10.0.0.5", "dev01", "3.3");
        vm.DiscoveredDevices.Add(device);
        vm.SelectedDevice = device;

        await ((IAsyncRelayCommand)vm.RenameDeviceCommand).ExecuteAsync(null);

        Assert.Null(vm.DiscoveredDevices[0].FriendlyName);
        _settingsSvc.Verify(s => s.SetFriendlyName(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    // ── ClearSavedDevice ──────────────────────────────────────────────────────

    [Fact]
    public void ClearSavedDevice_NullifiesSettingsAndSaves()
    {
        _appSettings = new AppSettings { DeviceIp = "10.0.0.5", DeviceId = "dev01" };

        var vm = CreateVm();
        vm.ClearSavedDeviceCommand.Execute(null);

        Assert.Null(_appSettings.DeviceIp);
        Assert.Null(_appSettings.DeviceId);
        Assert.Null(_appSettings.EncryptedLocalKey);
        _settingsSvc.Verify(s => s.Save(), Times.Once);
    }

    [Fact]
    public async Task ClearSavedDevice_WhenConnected_Disconnects()
    {
        var result = new ConnectDialogResult("10.0.0.5", "dev01", "key");
        _dialogSvc.Setup(d => d.ShowConnectDialogAsync(It.IsAny<string>(), It.IsAny<string>()))
                  .ReturnsAsync(result);
        _deviceSvc.Setup(s => s.ConnectAsync(It.IsAny<string>(), It.IsAny<string>(),
                             It.IsAny<string>(), It.IsAny<CancellationToken>()))
                  .Returns(Task.CompletedTask);
        _deviceSvc.Setup(s => s.GetStateAsync(It.IsAny<CancellationToken>()))
                  .ReturnsAsync(new DeviceState());

        var vm = CreateVm();
        await ((IAsyncRelayCommand)vm.ConnectCommand).ExecuteAsync(null);
        Assert.True(vm.IsConnected);

        vm.ClearSavedDeviceCommand.Execute(null);

        Assert.False(vm.IsConnected);
        _deviceSvc.Verify(s => s.Disconnect(), Times.Once);
    }
}
