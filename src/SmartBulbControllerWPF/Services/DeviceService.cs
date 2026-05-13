using com.clusterrr.TuyaNet;
using Microsoft.Extensions.Logging;
using SmartBulbControllerWPF.Interfaces;
using SmartBulbControllerWPF.Models;

namespace SmartBulbControllerWPF.Services;

public class DeviceService : ServiceBase, IDeviceService, IDisposable
{
    private const int DpPower       = 20;
    private const int DpMode        = 21;
    private const int DpBrightness  = 22;
    private const int DpColorTemp   = 23;
    private const int DpColor       = 24;

    private const int BrightnessMin = 10;
    private const int BrightnessMax = 1000;
    private const int ColorTempMin  = 0;
    private const int ColorTempMax  = 1000;

    private TuyaDevice? _device;

    public bool IsConnected => _device != null;

    public DeviceService(ILogger<DeviceService> logger) : base(logger) { }

    public async Task<IEnumerable<DiscoveredDevice>> ScanAsync(int scanSeconds = 5, CancellationToken ct = default)
    {
        var found = new List<DiscoveredDevice>();
        var scanner = new TuyaScanner();
        scanner.OnNewDeviceInfoReceived += (_, info) =>
            found.Add(new DiscoveredDevice(info.IP, info.GwId, info.Version));

        scanner.Start();
        try
        {
            await Task.Delay(TimeSpan.FromSeconds(scanSeconds), ct);
        }
        catch (OperationCanceledException) { }
        finally
        {
            scanner.Stop();
        }

        Logger.LogInformation("Scan found {Count} device(s)", found.Count);
        return found;
    }

    public Task ConnectAsync(string ip, string deviceId, string localKey, CancellationToken ct = default)
    {
        Disconnect();
        _device = new TuyaDevice(ip, localKey, deviceId, TuyaProtocolVersion.V33);
        Logger.LogInformation("Connected to device {DeviceId} at {Ip}", deviceId, ip);
        return Task.CompletedTask;
    }

    public void Disconnect()
    {
        _device?.Dispose();
        _device = null;
    }

    public async Task<DeviceState> GetStateAsync(CancellationToken ct = default)
    {
        EnsureConnected();
        var dps = await _device!.GetDpsAsync(cancellationToken: ct);
        return ParseState(dps);
    }

    public async Task SetPowerAsync(bool on, CancellationToken ct = default)
    {
        EnsureConnected();
        await _device!.SetDpAsync(DpPower, on, cancellationToken: ct);
        Logger.LogInformation("Power set to {State}", on ? "on" : "off");
    }

    public async Task SetColorAsync(byte r, byte g, byte b, CancellationToken ct = default)
    {
        EnsureConnected();
        var hsv = RgbToTuyaHsv(r, g, b);
        await _device!.SetDpsAsync(new Dictionary<int, object>
        {
            { DpMode,  "colour" },
            { DpColor, hsv },
        }, cancellationToken: ct);
        Logger.LogInformation("Color set to #{R:X2}{G:X2}{B:X2}", r, g, b);
    }

    public async Task SetBrightnessAsync(int percent, CancellationToken ct = default)
    {
        EnsureConnected();
        var value = ScaleToRange(percent, BrightnessMin, BrightnessMax);
        await _device!.SetDpsAsync(new Dictionary<int, object>
        {
            { DpMode,       "white" },
            { DpBrightness, value   },
        }, cancellationToken: ct);
        Logger.LogInformation("Brightness set to {Percent}%", percent);
    }

    public async Task SetColorTemperatureAsync(int percent, CancellationToken ct = default)
    {
        EnsureConnected();
        var value = ScaleToRange(percent, ColorTempMin, ColorTempMax);
        await _device!.SetDpsAsync(new Dictionary<int, object>
        {
            { DpMode,     "white" },
            { DpColorTemp, value  },
        }, cancellationToken: ct);
        Logger.LogInformation("Color temperature set to {Percent}%", percent);
    }

    public void Dispose() => Disconnect();

    // ── Helpers ──────────────────────────────────────────────────────────────

    private void EnsureConnected()
    {
        if (_device == null)
            throw new InvalidOperationException("Not connected to a device.");
    }

    private static DeviceState ParseState(Dictionary<int, object> dps)
    {
        var state = new DeviceState();
        if (dps.TryGetValue(DpPower,      out var power))  state.Power            = (bool)power;
        if (dps.TryGetValue(DpMode,       out var mode))   state.Mode             = mode?.ToString() ?? "white";
        if (dps.TryGetValue(DpBrightness, out var bright)) state.Brightness       = ScaleFromRange(Convert.ToInt32(bright), BrightnessMin, BrightnessMax);
        if (dps.TryGetValue(DpColorTemp,  out var temp))   state.ColorTemperature = ScaleFromRange(Convert.ToInt32(temp),   ColorTempMin,  ColorTempMax);
        if (dps.TryGetValue(DpColor,      out var colour) && colour is string hsv)
            state.Color = TuyaHsvToRgb(hsv);
        return state;
    }

    // Tuya HSV format: HHHHSSSSVVVV (12 hex chars)
    // H: 0-360, S: 0-1000, V: 0-1000
    internal static string RgbToTuyaHsv(byte r, byte g, byte b)
    {
        double rf = r / 255.0, gf = g / 255.0, bf = b / 255.0;
        double max = Math.Max(rf, Math.Max(gf, bf));
        double min = Math.Min(rf, Math.Min(gf, bf));
        double delta = max - min;

        double h = 0;
        if (delta > 0)
        {
            if (max == rf)      h = 60 * (((gf - bf) / delta) % 6);
            else if (max == gf) h = 60 * (((bf - rf) / delta) + 2);
            else                h = 60 * (((rf - gf) / delta) + 4);
        }
        if (h < 0) h += 360;

        double s = max == 0 ? 0 : delta / max;
        double v = max;

        int hi = (int)Math.Round(h);
        int si = (int)Math.Round(s * 1000);
        int vi = (int)Math.Round(v * 1000);

        return $"{hi:X4}{si:X4}{vi:X4}";
    }

    internal static (byte R, byte G, byte B) TuyaHsvToRgb(string hsv)
    {
        if (hsv.Length < 12) return (255, 255, 255);
        double h = Convert.ToInt32(hsv[..4],  16);
        double s = Convert.ToInt32(hsv[4..8], 16) / 1000.0;
        double v = Convert.ToInt32(hsv[8..],  16) / 1000.0;

        double c  = v * s;
        double x  = c * (1 - Math.Abs((h / 60) % 2 - 1));
        double m  = v - c;

        double rf, gf, bf;
        if      (h < 60)  { rf = c;  gf = x;  bf = 0; }
        else if (h < 120) { rf = x;  gf = c;  bf = 0; }
        else if (h < 180) { rf = 0;  gf = c;  bf = x; }
        else if (h < 240) { rf = 0;  gf = x;  bf = c; }
        else if (h < 300) { rf = x;  gf = 0;  bf = c; }
        else              { rf = c;  gf = 0;  bf = x; }

        return ((byte)((rf + m) * 255), (byte)((gf + m) * 255), (byte)((bf + m) * 255));
    }

    internal static int ScaleToRange(int percent, int min, int max)
        => min + (int)Math.Round((max - min) * Math.Clamp(percent, 0, 100) / 100.0);

    internal static int ScaleFromRange(int value, int min, int max)
        => (int)Math.Round((value - min) * 100.0 / (max - min));
}
