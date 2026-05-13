using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using SmartBulbControllerWPF.Interfaces;
using SmartBulbControllerWPF.Models;
using SmartBulbControllerWPF.Services;
using SmartBulbControllerWPF.ViewModels;

namespace SmartBulbControllerWPF.Tests.ViewModels;

internal static class ColorControlTestHelper
{
    internal static MainViewModel CreateVm()
    {
        var settings = new Mock<ISettingsService>();
        settings.SetupGet(s => s.Current).Returns(new AppSettings());
        settings.Setup(s => s.GetLocalKey()).Returns((string?)null);
        settings.Setup(s => s.GetFriendlyName(It.IsAny<string>())).Returns((string?)null);

        var presets = new Mock<IPresetService>();
        presets.SetupGet(p => p.Presets).Returns([]);

        return new MainViewModel(
            new Mock<IDeviceService>().Object,
            settings.Object,
            new Mock<IDialogService>().Object,
            presets.Object,
            new Mock<IThemeService>().Object,
            new Mock<IAlertService>().Object,
            new Mock<ISceneService>().Object,
            new StartupService(NullLogger<StartupService>.Instance),
            NullLogger<MainViewModel>.Instance);
    }
}
