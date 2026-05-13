using Microsoft.Extensions.Logging;
using SmartBulbControllerWPF.Interfaces;
using SmartBulbControllerWPF.Models;

namespace SmartBulbControllerWPF.Services;

public class ScheduleService : ServiceBase, IScheduleService, IDisposable
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(30);

    private readonly IDeviceService  _deviceService;
    private readonly ISettingsService _settingsService;
    private readonly List<ScheduleEntry> _entries;
    private CancellationTokenSource? _cts;

    public IReadOnlyList<ScheduleEntry> Entries => _entries;

    public ScheduleService(
        IDeviceService  deviceService,
        ISettingsService settingsService,
        ILogger<ScheduleService> logger) : base(logger)
    {
        _deviceService   = deviceService;
        _settingsService = settingsService;
        _entries         = [.. settingsService.Current.ScheduleEntries];
    }

    public void Add(ScheduleEntry entry)
    {
        _entries.Add(entry);
        Persist();
    }

    public void Remove(Guid id)
    {
        _entries.RemoveAll(e => e.Id == id);
        Persist();
    }

    public void SetEnabled(Guid id, bool enabled)
    {
        var entry = _entries.FirstOrDefault(e => e.Id == id);
        if (entry is null) return;
        entry.IsEnabled = enabled;
        Persist();
    }

    public void Start()
    {
        if (_cts is not null) return;
        _cts = new CancellationTokenSource();
        _ = RunLoopAsync(_cts.Token);
    }

    public void Stop()
    {
        _cts?.Cancel();
        _cts?.Dispose();
        _cts = null;
    }

    public void Dispose() => Stop();

    // ── Poll loop ─────────────────────────────────────────────────────────────

    private async Task RunLoopAsync(CancellationToken ct)
    {
        try
        {
            using var timer = new PeriodicTimer(PollInterval);
            while (await timer.WaitForNextTickAsync(ct))
                await CheckEntriesAsync(ct);
        }
        catch (OperationCanceledException) { }
    }

    private async Task CheckEntriesAsync(CancellationToken ct)
    {
        if (!_deviceService.IsConnected) return;

        var now   = DateTime.Now;
        var today = DateTime.Today;

        foreach (var entry in _entries.ToList())
        {
            if (!entry.IsEnabled) continue;

            var scheduledToday = today + entry.Time;

            // Fire if we just crossed the scheduled time in this poll window
            if (now >= scheduledToday && now < scheduledToday + PollInterval)
            {
                if (entry.LastFired?.Date == today) continue; // already fired today

                Logger.LogInformation("Schedule firing: {Action} at {Time}",
                    entry.TurnOn ? "on" : "off", entry.Time);

                await ExecuteAsync(
                    () => _deviceService.SetPowerAsync(entry.TurnOn, ct),
                    "schedule power");

                entry.LastFired = scheduledToday;

                if (!entry.IsDaily)
                {
                    entry.IsEnabled = false;
                    Logger.LogInformation("One-shot schedule entry {Id} disabled after firing", entry.Id);
                }

                Persist();
            }
        }
    }

    private void Persist()
    {
        _settingsService.Current.ScheduleEntries = [.. _entries];
        _settingsService.Save();
    }
}
