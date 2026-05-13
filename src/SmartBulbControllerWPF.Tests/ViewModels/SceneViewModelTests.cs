using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using SmartBulbControllerWPF.Interfaces;
using SmartBulbControllerWPF.Models;
using SmartBulbControllerWPF.Services;
using SmartBulbControllerWPF.ViewModels;

namespace SmartBulbControllerWPF.Tests.ViewModels;

public class SceneViewModelTests
{
    private readonly Mock<IDeviceService>   _deviceSvc   = new();
    private readonly Mock<ISettingsService> _settingsSvc = new();
    private readonly Mock<ISceneService>    _sceneSvc    = new();

    private MainViewModel CreateVm()
    {
        _settingsSvc.SetupGet(s => s.Current).Returns(new AppSettings());
        _settingsSvc.Setup(s => s.GetLocalKey()).Returns((string?)null);
        _settingsSvc.Setup(s => s.GetFriendlyName(It.IsAny<string>())).Returns((string?)null);

        var presets = new Mock<IPresetService>();
        presets.SetupGet(p => p.Presets).Returns([]);

        return new MainViewModel(
            _deviceSvc.Object,
            _settingsSvc.Object,
            new Mock<IDialogService>().Object,
            presets.Object,
            new Mock<IThemeService>().Object,
            new Mock<IAlertService>().Object,
            _sceneSvc.Object,
            new StartupService(NullLogger<StartupService>.Instance),
            NullLogger<MainViewModel>.Instance);
    }

    private async Task ConnectVmAsync(MainViewModel vm)
    {
        var result = new ConnectDialogResult("10.0.0.1", "dev01", "key");
        var dialog = new Mock<IDialogService>();
        dialog.Setup(d => d.ShowConnectDialogAsync(It.IsAny<string?>(), It.IsAny<string?>()))
              .ReturnsAsync(result);

        _deviceSvc.Setup(d => d.ConnectAsync(It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<string>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _deviceSvc.Setup(d => d.GetStateAsync(It.IsAny<CancellationToken>()))
              .ReturnsAsync(new DeviceState());
        _settingsSvc.Setup(s => s.SetLocalKey(It.IsAny<string>()));
        _settingsSvc.Setup(s => s.Save());

        await ((IAsyncRelayCommand)vm.ConnectCommand).ExecuteAsync(null);
    }

    [Fact]
    public void InitialActiveScene_IsNone()
    {
        var vm = CreateVm();
        Assert.Equal(SceneType.None, vm.ActiveScene);
    }

    [Fact]
    public async Task SetScene_WhenConnected_StartsScene()
    {
        var vm = CreateVm();
        await ConnectVmAsync(vm);

        vm.SetSceneCommand.Execute(SceneType.ColorCycle);

        _sceneSvc.Verify(s => s.Start(SceneType.ColorCycle,
            It.IsAny<byte>(), It.IsAny<byte>(), It.IsAny<byte>(),
            It.IsAny<int>()), Times.Once);
        Assert.Equal(SceneType.ColorCycle, vm.ActiveScene);
        Assert.True(vm.IsColorCycleActive);
    }

    [Fact]
    public async Task SetScene_SameScene_StopsIt()
    {
        var vm = CreateVm();
        await ConnectVmAsync(vm);

        vm.SetSceneCommand.Execute(SceneType.Pulse);
        vm.SetSceneCommand.Execute(SceneType.Pulse);

        _sceneSvc.Verify(s => s.Stop(), Times.AtLeastOnce);
        Assert.Equal(SceneType.None, vm.ActiveScene);
    }

    [Fact]
    public async Task SetScene_None_StopsAnyScene()
    {
        var vm = CreateVm();
        await ConnectVmAsync(vm);

        vm.SetSceneCommand.Execute(SceneType.Strobe);
        vm.SetSceneCommand.Execute(SceneType.None);

        _sceneSvc.Verify(s => s.Stop(), Times.AtLeastOnce);
        Assert.Equal(SceneType.None, vm.ActiveScene);
    }

    [Fact]
    public async Task Disconnect_StopsRunningScene()
    {
        var vm = CreateVm();
        await ConnectVmAsync(vm);

        vm.SetSceneCommand.Execute(SceneType.ColorCycle);
        vm.DisconnectCommand.Execute(null);

        _sceneSvc.Verify(s => s.Stop(), Times.AtLeastOnce);
        Assert.Equal(SceneType.None, vm.ActiveScene);
    }

    [Fact]
    public void IsColorCycleActive_ReflectsActiveScene()
    {
        var vm = CreateVm();
        Assert.False(vm.IsColorCycleActive);
        Assert.False(vm.IsPulseActive);
        Assert.False(vm.IsStrobeActive);
    }

    [Fact]
    public void SetScene_CanExecute_OnlyWhenConnected()
    {
        var vm = CreateVm();
        Assert.False(vm.SetSceneCommand.CanExecute(SceneType.Pulse));
    }
}
