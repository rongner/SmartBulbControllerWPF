using Microsoft.Extensions.Logging;
using SmartBulbControllerWPF.Interfaces;
using SmartBulbControllerWPF.Models;

namespace SmartBulbControllerWPF.Services;

public class AlertService : ServiceBase, IAlertService
{
    private readonly IDeviceService       _device;
    private readonly ISettingsService     _settings;
    private readonly EspnScheduleService  _espn;

    private CancellationTokenSource? _cts;
    private bool _alertFired;
    private DateTime? _nextGameTime;

    public bool IsRunning => _cts is { IsCancellationRequested: false };
    public DateTime? NextGameTime => _nextGameTime;
    public event Action? NextGameUpdated;

    public AlertService(
        IDeviceService      device,
        ISettingsService    settings,
        EspnScheduleService espn,
        ILogger<AlertService> logger) : base(logger)
    {
        _device   = device;
        _settings = settings;
        _espn     = espn;
    }

    public void Start()
    {
        if (IsRunning) return;
        _cts = new CancellationTokenSource();
        _ = RunLoopAsync(_cts.Token);
        Logger.LogInformation("Alert service started");
    }

    public void Stop()
    {
        _cts?.Cancel();
        Logger.LogInformation("Alert service stopped");
    }

    private async Task RunLoopAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                await TickAsync(ct);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Alert loop error");
            }

            await Task.Delay(TimeSpan.FromMinutes(5), ct);
        }
    }

    private async Task TickAsync(CancellationToken ct)
    {
        var cfg = _settings.Current;
        if (!cfg.AlertEnabled || cfg.NbaTeamId is null) return;
        if (!_device.IsConnected) return;

        var team = NbaTeams.All.FirstOrDefault(t => t.Id == cfg.NbaTeamId.Value);
        if (team is null) return;

        var nextGame = await _espn.GetNextGameTimeAsync(team.Id, ct);
        if (_nextGameTime != nextGame)
        {
            _nextGameTime = nextGame;
            NextGameUpdated?.Invoke();
        }
        if (nextGame is null) return;

        var leadTime    = TimeSpan.FromMinutes(cfg.LeadTimeMinutes);
        var alertWindow = TimeSpan.FromMinutes(10);
        var timeUntil   = nextGame.Value - DateTime.Now;

        if (timeUntil <= leadTime && timeUntil > leadTime - alertWindow && !_alertFired)
        {
            Logger.LogInformation("Pre-game alert: {Team} in {Minutes:0} min", team.Name, timeUntil.TotalMinutes);
            _alertFired = true;

            await _device.SetColorAsync(team.R, team.G, team.B);
            await _device.SetBrightnessAsync(cfg.AlertBrightness);

            _ = ScheduleRevertAsync(cfg.RevertAfterHours, ct);
        }
        else if (timeUntil > leadTime)
        {
            _alertFired = false;
        }
    }

    private async Task ScheduleRevertAsync(int hours, CancellationToken ct)
    {
        try
        {
            await Task.Delay(TimeSpan.FromHours(hours), ct);
            if (!_device.IsConnected) return;
            Logger.LogInformation("Reverting light after game");
            await _device.SetColorTemperatureAsync(50);
            await _device.SetBrightnessAsync(80);
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to revert light after game");
        }
    }
}
