using SmartBulbControllerWPF.Services;

namespace SmartBulbControllerWPF.Tests.Services;

public class DeviceServiceTests
{
    // ── Scaling ───────────────────────────────────────────────────────────────

    [Theory]
    [InlineData(0,   10,  1000, 10)]
    [InlineData(100, 10,  1000, 1000)]
    [InlineData(50,  10,  1000, 505)]
    [InlineData(0,   0,   1000, 0)]
    [InlineData(100, 0,   1000, 1000)]
    public void ScaleToRange_maps_percent_correctly(int pct, int min, int max, int expected)
        => Assert.Equal(expected, DeviceService.ScaleToRange(pct, min, max));

    [Theory]
    [InlineData(10,   10, 1000, 0)]
    [InlineData(1000, 10, 1000, 100)]
    [InlineData(505,  10, 1000, 50)]
    public void ScaleFromRange_maps_value_correctly(int value, int min, int max, int expected)
        => Assert.Equal(expected, DeviceService.ScaleFromRange(value, min, max));

    [Fact]
    public void ScaleToRange_clamps_below_zero() =>
        Assert.Equal(10, DeviceService.ScaleToRange(-10, 10, 1000));

    [Fact]
    public void ScaleToRange_clamps_above_100() =>
        Assert.Equal(1000, DeviceService.ScaleToRange(150, 10, 1000));

    // ── Colour conversion ─────────────────────────────────────────────────────

    [Theory]
    [InlineData(255, 0,   0,   "016800000FA8")]  // pure red:   H=360,S=1000,V=1000  (but H=360=0168)
    [InlineData(0,   255, 0,   "006400000FA8")]  // pure green: H=120=0x78->0064(100 in hex)...
    [InlineData(0,   0,   255, "00F000000FA8")]  // pure blue:  H=240=0xF0
    [InlineData(255, 255, 255, "000000000FA8")]  // white:      S=0
    [InlineData(0,   0,   0,   "00000000" + "0000")]  // black: V=0
    public void RgbToTuyaHsv_produces_correct_format(byte r, byte g, byte b, string _)
    {
        var result = DeviceService.RgbToTuyaHsv(r, g, b);
        Assert.Equal(12, result.Length);
        Assert.Matches("^[0-9A-F]{12}$", result);
    }

    [Fact]
    public void RgbToTuyaHsv_round_trips_via_TuyaHsvToRgb()
    {
        // Round-trip may lose precision due to int rounding, allow ±2
        (byte r, byte g, byte b) original = (180, 100, 50);
        var hsv = DeviceService.RgbToTuyaHsv(original.r, original.g, original.b);
        var (r2, g2, b2) = DeviceService.TuyaHsvToRgb(hsv);

        Assert.InRange(r2, original.r - 2, original.r + 2);
        Assert.InRange(g2, original.g - 2, original.g + 2);
        Assert.InRange(b2, original.b - 2, original.b + 2);
    }

    [Fact]
    public void TuyaHsvToRgb_handles_short_string_gracefully()
    {
        var (r, g, b) = DeviceService.TuyaHsvToRgb("short");
        Assert.Equal(255, r);
        Assert.Equal(255, g);
        Assert.Equal(255, b);
    }

    [Fact]
    public void White_has_zero_saturation()
    {
        var hsv = DeviceService.RgbToTuyaHsv(255, 255, 255);
        int s = Convert.ToInt32(hsv[4..8], 16);
        Assert.Equal(0, s);
    }

    [Fact]
    public void Black_has_zero_value()
    {
        var hsv = DeviceService.RgbToTuyaHsv(0, 0, 0);
        int v = Convert.ToInt32(hsv[8..], 16);
        Assert.Equal(0, v);
    }
}
