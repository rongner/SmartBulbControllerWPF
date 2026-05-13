using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using SmartBulbControllerWPF.Interfaces;
using SmartBulbControllerWPF.Models;
using SmartBulbControllerWPF.Services;
using SmartBulbControllerWPF.ViewModels;

namespace SmartBulbControllerWPF.Tests.ViewModels;

public class GroupViewModelTests
{
    private readonly Mock<IDeviceService>   _deviceSvc   = new();
    private readonly Mock<ISettingsService> _settingsSvc = new();
    private readonly Mock<IDialogService>   _dialogSvc   = new();

    private MainViewModel CreateVm()
    {
        _settingsSvc.SetupGet(s => s.Current).Returns(new AppSettings());
        _settingsSvc.Setup(s => s.GetLocalKey()).Returns((string?)null);
        _settingsSvc.Setup(s => s.GetFriendlyName(It.IsAny<string>())).Returns((string?)null);
        _deviceSvc.SetupGet(d => d.GroupMemberIps).Returns([]);

        var presets = new Mock<IPresetService>();
        presets.SetupGet(p => p.Presets).Returns([]);
        var schedule = new Mock<IScheduleService>();
        schedule.SetupGet(s => s.Entries).Returns([]);

        return new MainViewModel(
            _deviceSvc.Object,
            _settingsSvc.Object,
            _dialogSvc.Object,
            presets.Object,
            new Mock<IThemeService>().Object,
            new Mock<IAlertService>().Object,
            new Mock<ISceneService>().Object,
            schedule.Object,
            new StartupService(NullLogger<StartupService>.Instance),
            NullLogger<MainViewModel>.Instance);
    }

    private async Task ConnectVmAsync(MainViewModel vm)
    {
        var result = new ConnectDialogResult("10.0.0.1", "dev01", "key");
        _dialogSvc.Setup(d => d.ShowConnectDialogAsync(It.IsAny<string?>(), It.IsAny<string?>()))
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
    public void AddToGroup_CannotExecute_WhenNotConnected()
    {
        var vm = CreateVm();
        vm.SelectedDevice = new DiscoveredDevice("10.0.0.2", "dev02", "3.3");

        Assert.False(vm.AddToGroupCommand.CanExecute(null));
    }

    [Fact]
    public async Task AddToGroup_CallsDeviceService()
    {
        var vm = CreateVm();
        await ConnectVmAsync(vm);

        var secondary = new DiscoveredDevice("10.0.0.2", "dev02", "3.3");
        vm.DiscoveredDevices.Add(secondary);
        vm.SelectedDevice = secondary;

        _dialogSvc.Setup(d => d.ShowConnectDialogAsync("10.0.0.2", "dev02"))
                  .ReturnsAsync(new ConnectDialogResult("10.0.0.2", "dev02", "key2"));
        _deviceSvc.Setup(d => d.AddToGroupAsync("10.0.0.2", "dev02", "key2",
            It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _deviceSvc.SetupGet(d => d.GroupMemberIps).Returns(["10.0.0.2"]);

        await ((IAsyncRelayCommand)vm.AddToGroupCommand).ExecuteAsync(null);

        _deviceSvc.Verify(d => d.AddToGroupAsync("10.0.0.2", "dev02", "key2",
            It.IsAny<CancellationToken>()), Times.Once);
        Assert.Contains("10.0.0.2", vm.GroupMemberIps);
    }

    [Fact]
    public async Task RemoveFromGroup_CallsDeviceService()
    {
        var vm = CreateVm();
        await ConnectVmAsync(vm);

        var secondary = new DiscoveredDevice("10.0.0.3", "dev03", "3.3");
        vm.DiscoveredDevices.Add(secondary);
        vm.GroupMemberIps.Add("10.0.0.3");
        vm.SelectedDevice = secondary;
        _deviceSvc.SetupGet(d => d.GroupMemberIps).Returns(["10.0.0.3"]);

        vm.RemoveFromGroupCommand.Execute(null);

        _deviceSvc.Verify(d => d.RemoveFromGroup("10.0.0.3"), Times.Once);
        Assert.DoesNotContain("10.0.0.3", vm.GroupMemberIps);
    }

    [Fact]
    public async Task Disconnect_ClearsGroupMemberIps()
    {
        var vm = CreateVm();
        await ConnectVmAsync(vm);

        vm.GroupMemberIps.Add("10.0.0.4");

        vm.DisconnectCommand.Execute(null);

        Assert.Empty(vm.GroupMemberIps);
    }
}
