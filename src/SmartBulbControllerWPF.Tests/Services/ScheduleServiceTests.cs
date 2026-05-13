using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using SmartBulbControllerWPF.Interfaces;
using SmartBulbControllerWPF.Models;
using SmartBulbControllerWPF.Services;

namespace SmartBulbControllerWPF.Tests.Services;

public class ScheduleServiceTests
{
    private readonly Mock<IDeviceService>  _deviceSvc   = new();
    private readonly Mock<ISettingsService> _settingsSvc = new();

    private ScheduleService Create(IEnumerable<ScheduleEntry>? entries = null)
    {
        var settings = new AppSettings();
        settings.ScheduleEntries.AddRange(entries ?? []);
        _settingsSvc.SetupGet(s => s.Current).Returns(settings);
        _settingsSvc.Setup(s => s.Save());
        return new ScheduleService(
            _deviceSvc.Object, _settingsSvc.Object, NullLogger<ScheduleService>.Instance);
    }

    [Fact]
    public void InitialEntries_LoadedFromSettings()
    {
        var entry = new ScheduleEntry { Time = TimeSpan.FromHours(7), TurnOn = true };
        var svc   = Create([entry]);

        Assert.Single(svc.Entries);
        Assert.Equal(entry.Id, svc.Entries[0].Id);
    }

    [Fact]
    public void Add_AppendsEntry()
    {
        var svc   = Create();
        var entry = new ScheduleEntry { Time = TimeSpan.FromHours(8), TurnOn = true };

        svc.Add(entry);

        Assert.Single(svc.Entries);
        _settingsSvc.Verify(s => s.Save(), Times.Once);
    }

    [Fact]
    public void Remove_DeletesEntry()
    {
        var entry = new ScheduleEntry { Time = TimeSpan.FromHours(9) };
        var svc   = Create([entry]);

        svc.Remove(entry.Id);

        Assert.Empty(svc.Entries);
        _settingsSvc.Verify(s => s.Save(), Times.Once);
    }

    [Fact]
    public void Remove_UnknownId_DoesNotThrow()
    {
        var svc = Create();
        var ex  = Record.Exception(() => svc.Remove(Guid.NewGuid()));
        Assert.Null(ex);
    }

    [Fact]
    public void SetEnabled_TogglesEntry()
    {
        var entry = new ScheduleEntry { Time = TimeSpan.FromHours(10), IsEnabled = true };
        var svc   = Create([entry]);

        svc.SetEnabled(entry.Id, false);

        Assert.False(svc.Entries[0].IsEnabled);
        _settingsSvc.Verify(s => s.Save(), Times.Once);
    }

    [Fact]
    public void Stop_WhenNotStarted_DoesNotThrow()
    {
        var svc = Create();
        var ex  = Record.Exception(() => svc.Stop());
        Assert.Null(ex);
    }

    [Fact]
    public void StartStop_DoesNotThrow()
    {
        var svc = Create();
        svc.Start();
        var ex = Record.Exception(() => svc.Stop());
        Assert.Null(ex);
    }
}
