using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using SmartBulbControllerWPF.Interfaces;
using SmartBulbControllerWPF.Models;
using SmartBulbControllerWPF.ViewModels;

namespace SmartBulbControllerWPF.Tests.ViewModels;

public class MainViewModelTests
{
    private readonly Mock<IDeviceService>   _deviceSvc  = new();
    private readonly Mock<ISettingsService> _settingsSvc = new();
    private readonly Mock<IDialogService>   _dialogSvc  = new();

    private MainViewModel CreateVm()
    {
        _settingsSvc.SetupGet(s => s.Current).Returns(new AppSettings());
        return new MainViewModel(
            _deviceSvc.Object,
            _settingsSvc.Object,
            _dialogSvc.Object,
            NullLogger<MainViewModel>.Instance);
    }

    // ── Scan ──────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Scan_PopulatesDevicesAndUpdatesStatus()
    {
        var devices = new[]
        {
            new DiscoveredDevice("192.168.1.10", "abc123", "3.3"),
            new DiscoveredDevice("192.168.1.11", "def456", "3.4"),
        };
        _deviceSvc.Setup(s => s.ScanAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                  .ReturnsAsync(devices);

        var vm = CreateVm();
        await ((IAsyncRelayCommand)vm.ScanCommand).ExecuteAsync(null);

        Assert.Equal(2, vm.DiscoveredDevices.Count);
        Assert.Contains("2", vm.StatusText);
    }

    [Fact]
    public async Task Scan_NoDevices_SetsNoDevicesStatus()
    {
        _deviceSvc.Setup(s => s.ScanAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                  .ReturnsAsync([]);

        var vm = CreateVm();
        await ((IAsyncRelayCommand)vm.ScanCommand).ExecuteAsync(null);

        Assert.Empty(vm.DiscoveredDevices);
        Assert.Equal("No devices found", vm.StatusText);
    }

    [Fact]
    public async Task Scan_ServiceThrows_SetsErrorMessage()
    {
        _deviceSvc.Setup(s => s.ScanAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                  .ThrowsAsync(new InvalidOperationException("network error"));

        var vm = CreateVm();
        await ((IAsyncRelayCommand)vm.ScanCommand).ExecuteAsync(null);

        Assert.Equal("network error", vm.ErrorMessage);
        Assert.False(vm.IsBusy);
    }

    [Fact]
    public async Task Scan_ClearsPreviousDevices()
    {
        _deviceSvc.SetupSequence(s => s.ScanAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                  .ReturnsAsync([new DiscoveredDevice("1.2.3.4", "aaa", "3.3")])
                  .ReturnsAsync([]);

        var vm = CreateVm();
        await ((IAsyncRelayCommand)vm.ScanCommand).ExecuteAsync(null);
        await ((IAsyncRelayCommand)vm.ScanCommand).ExecuteAsync(null);

        Assert.Empty(vm.DiscoveredDevices);
    }

    // ── Connect ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task Connect_DialogCancelled_DoesNotConnect()
    {
        _dialogSvc.Setup(d => d.ShowConnectDialogAsync(It.IsAny<string>(), It.IsAny<string>()))
                  .ReturnsAsync((ConnectDialogResult?)null);

        var vm = CreateVm();
        await ((IAsyncRelayCommand)vm.ConnectCommand).ExecuteAsync(null);

        _deviceSvc.Verify(s => s.ConnectAsync(It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        Assert.False(vm.IsConnected);
    }

    [Fact]
    public async Task Connect_Success_SetsConnectedStateAndSavesSettings()
    {
        var result = new ConnectDialogResult("10.0.0.5", "dev01", "secret");
        var state  = new DeviceState { Power = true };

        _dialogSvc.Setup(d => d.ShowConnectDialogAsync(It.IsAny<string>(), It.IsAny<string>()))
                  .ReturnsAsync(result);
        _deviceSvc.Setup(s => s.ConnectAsync("10.0.0.5", "dev01", "secret", It.IsAny<CancellationToken>()))
                  .Returns(Task.CompletedTask);
        _deviceSvc.Setup(s => s.GetStateAsync(It.IsAny<CancellationToken>()))
                  .ReturnsAsync(state);

        var vm = CreateVm();
        await ((IAsyncRelayCommand)vm.ConnectCommand).ExecuteAsync(null);

        Assert.True(vm.IsConnected);
        Assert.True(vm.IsOn);
        Assert.Contains("10.0.0.5", vm.StatusText);
        _settingsSvc.Verify(s => s.SetLocalKey("secret"), Times.Once);
        _settingsSvc.Verify(s => s.Save(), Times.Once);
    }

    [Fact]
    public async Task Connect_DeviceThrows_SetsErrorAndNotConnected()
    {
        var result = new ConnectDialogResult("10.0.0.5", "dev01", "secret");

        _dialogSvc.Setup(d => d.ShowConnectDialogAsync(It.IsAny<string>(), It.IsAny<string>()))
                  .ReturnsAsync(result);
        _deviceSvc.Setup(s => s.ConnectAsync(It.IsAny<string>(), It.IsAny<string>(),
                             It.IsAny<string>(), It.IsAny<CancellationToken>()))
                  .ThrowsAsync(new Exception("connection refused"));

        var vm = CreateVm();
        await ((IAsyncRelayCommand)vm.ConnectCommand).ExecuteAsync(null);

        Assert.False(vm.IsConnected);
        Assert.Equal("connection refused", vm.ErrorMessage);
    }

    [Fact]
    public async Task Connect_CannotExecuteWhenAlreadyConnected()
    {
        var result = new ConnectDialogResult("10.0.0.5", "dev01", "secret");
        var state  = new DeviceState();

        _dialogSvc.Setup(d => d.ShowConnectDialogAsync(It.IsAny<string>(), It.IsAny<string>()))
                  .ReturnsAsync(result);
        _deviceSvc.Setup(s => s.ConnectAsync(It.IsAny<string>(), It.IsAny<string>(),
                             It.IsAny<string>(), It.IsAny<CancellationToken>()))
                  .Returns(Task.CompletedTask);
        _deviceSvc.Setup(s => s.GetStateAsync(It.IsAny<CancellationToken>()))
                  .ReturnsAsync(state);

        var vm = CreateVm();
        await ((IAsyncRelayCommand)vm.ConnectCommand).ExecuteAsync(null);

        Assert.False(vm.ConnectCommand.CanExecute(null));
    }

    // ── Disconnect ────────────────────────────────────────────────────────────

    [Fact]
    public async Task Disconnect_ResetsConnectionState()
    {
        // Connect first
        var result = new ConnectDialogResult("10.0.0.5", "dev01", "secret");
        _dialogSvc.Setup(d => d.ShowConnectDialogAsync(It.IsAny<string>(), It.IsAny<string>()))
                  .ReturnsAsync(result);
        _deviceSvc.Setup(s => s.ConnectAsync(It.IsAny<string>(), It.IsAny<string>(),
                             It.IsAny<string>(), It.IsAny<CancellationToken>()))
                  .Returns(Task.CompletedTask);
        _deviceSvc.Setup(s => s.GetStateAsync(It.IsAny<CancellationToken>()))
                  .ReturnsAsync(new DeviceState { Power = true });

        var vm = CreateVm();
        await ((IAsyncRelayCommand)vm.ConnectCommand).ExecuteAsync(null);

        vm.DisconnectCommand.Execute(null);

        Assert.False(vm.IsConnected);
        Assert.False(vm.IsOn);
        Assert.Equal("Not connected", vm.StatusText);
        _deviceSvc.Verify(s => s.Disconnect(), Times.Once);
    }

    [Fact]
    public async Task Disconnect_DoesNotSendPowerChangeToDevice()
    {
        // Connect
        var result = new ConnectDialogResult("10.0.0.5", "dev01", "secret");
        _dialogSvc.Setup(d => d.ShowConnectDialogAsync(It.IsAny<string>(), It.IsAny<string>()))
                  .ReturnsAsync(result);
        _deviceSvc.Setup(s => s.ConnectAsync(It.IsAny<string>(), It.IsAny<string>(),
                             It.IsAny<string>(), It.IsAny<CancellationToken>()))
                  .Returns(Task.CompletedTask);
        _deviceSvc.Setup(s => s.GetStateAsync(It.IsAny<CancellationToken>()))
                  .ReturnsAsync(new DeviceState { Power = true });

        var vm = CreateVm();
        await ((IAsyncRelayCommand)vm.ConnectCommand).ExecuteAsync(null);

        vm.DisconnectCommand.Execute(null);

        // IsOn=false set during disconnect must not trigger SetPowerAsync
        _deviceSvc.Verify(s => s.SetPowerAsync(It.IsAny<bool>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    // ── Power toggle ──────────────────────────────────────────────────────────

    [Fact]
    public async Task IsOnChange_WhenConnected_CallsSetPowerAsync()
    {
        // Connect
        var result = new ConnectDialogResult("10.0.0.5", "dev01", "secret");
        _dialogSvc.Setup(d => d.ShowConnectDialogAsync(It.IsAny<string>(), It.IsAny<string>()))
                  .ReturnsAsync(result);
        _deviceSvc.Setup(s => s.ConnectAsync(It.IsAny<string>(), It.IsAny<string>(),
                             It.IsAny<string>(), It.IsAny<CancellationToken>()))
                  .Returns(Task.CompletedTask);
        _deviceSvc.Setup(s => s.GetStateAsync(It.IsAny<CancellationToken>()))
                  .ReturnsAsync(new DeviceState { Power = false });
        _deviceSvc.Setup(s => s.SetPowerAsync(It.IsAny<bool>(), It.IsAny<CancellationToken>()))
                  .Returns(Task.CompletedTask);

        var vm = CreateVm();
        await ((IAsyncRelayCommand)vm.ConnectCommand).ExecuteAsync(null);

        vm.IsOn = true;
        await Task.Delay(50); // let fire-and-forget complete

        _deviceSvc.Verify(s => s.SetPowerAsync(true, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public void IsOnChange_WhenNotConnected_DoesNotCallSetPowerAsync()
    {
        var vm = CreateVm();
        vm.IsOn = true;

        _deviceSvc.Verify(s => s.SetPowerAsync(It.IsAny<bool>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    // ── About ─────────────────────────────────────────────────────────────────

    [Fact]
    public async Task ShowAbout_CallsDialogService()
    {
        _dialogSvc.Setup(d => d.ShowAboutAsync()).Returns(Task.CompletedTask);

        var vm = CreateVm();
        await ((IAsyncRelayCommand)vm.ShowAboutCommand).ExecuteAsync(null);

        _dialogSvc.Verify(d => d.ShowAboutAsync(), Times.Once);
    }
}
