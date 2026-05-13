using Microsoft.Extensions.Logging;
using SmartBulbControllerWPF.Helpers;
using SmartBulbControllerWPF.Interfaces;
using SmartBulbControllerWPF.Models;

namespace SmartBulbControllerWPF.Services;

public class SceneService : ServiceBase, ISceneService
{
    private readonly IDeviceService _deviceService;
    private CancellationTokenSource? _cts;

    public SceneType ActiveScene { get; private set; }

    public SceneService(IDeviceService deviceService, ILogger<SceneService> logger)
        : base(logger)
    {
        _deviceService = deviceService;
    }

    public void Start(SceneType scene, byte r, byte g, byte b, int stepMs)
    {
        Stop();
        if (scene == SceneType.None) return;

        _cts = new CancellationTokenSource();
        ActiveScene  = scene;
        var token    = _cts.Token;
        var stepCopy = Math.Max(30, stepMs);

        _ = scene switch
        {
            SceneType.ColorCycle => RunColorCycleAsync(stepCopy, token),
            SceneType.Pulse      => RunPulseAsync(r, g, b, stepCopy, token),
            SceneType.Strobe     => RunStrobeAsync(r, g, b, stepCopy, token),
            _                    => Task.CompletedTask,
        };
    }

    public void Stop()
    {
        _cts?.Cancel();
        _cts?.Dispose();
        _cts        = null;
        ActiveScene = SceneType.None;
    }

    private async Task RunColorCycleAsync(int stepMs, CancellationToken ct)
    {
        double hue = 0;
        try
        {
            while (!ct.IsCancellationRequested)
            {
                var (r, g, b) = ColorHelper.HsvToRgb(hue, 1.0, 1.0);
                try { await _deviceService.SetColorAsync(r, g, b, ct); }
                catch (OperationCanceledException) { return; }
                catch (Exception ex) { Logger.LogWarning(ex, "Color cycle step failed"); }

                hue = (hue + 2) % 360;
                await Task.Delay(stepMs, ct);
            }
        }
        catch (OperationCanceledException) { }
    }

    private async Task RunPulseAsync(byte r, byte g, byte b, int stepMs, CancellationToken ct)
    {
        try
        {
            try { await _deviceService.SetColorAsync(r, g, b, ct); }
            catch (OperationCanceledException) { return; }

            while (!ct.IsCancellationRequested)
            {
                for (int brightness = 10; brightness <= 100 && !ct.IsCancellationRequested; brightness += 5)
                {
                    try { await _deviceService.SetBrightnessAsync(brightness, ct); }
                    catch (OperationCanceledException) { return; }
                    catch (Exception ex) { Logger.LogWarning(ex, "Pulse step failed"); }
                    await Task.Delay(stepMs, ct);
                }
                for (int brightness = 95; brightness >= 10 && !ct.IsCancellationRequested; brightness -= 5)
                {
                    try { await _deviceService.SetBrightnessAsync(brightness, ct); }
                    catch (OperationCanceledException) { return; }
                    catch (Exception ex) { Logger.LogWarning(ex, "Pulse step failed"); }
                    await Task.Delay(stepMs, ct);
                }
            }
        }
        catch (OperationCanceledException) { }
    }

    private async Task RunStrobeAsync(byte r, byte g, byte b, int stepMs, CancellationToken ct)
    {
        try
        {
            try { await _deviceService.SetColorAsync(r, g, b, ct); }
            catch (OperationCanceledException) { return; }

            bool on = true;
            while (!ct.IsCancellationRequested)
            {
                try { await _deviceService.SetBrightnessAsync(on ? 100 : 0, ct); }
                catch (OperationCanceledException) { return; }
                catch (Exception ex) { Logger.LogWarning(ex, "Strobe step failed"); }
                on = !on;
                await Task.Delay(stepMs, ct);
            }
        }
        catch (OperationCanceledException) { }
    }
}
