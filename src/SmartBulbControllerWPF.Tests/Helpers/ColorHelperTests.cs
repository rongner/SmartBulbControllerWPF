using SmartBulbControllerWPF.Helpers;

namespace SmartBulbControllerWPF.Tests.Helpers;

public class ColorHelperTests
{
    // ── HsvToRgb ──────────────────────────────────────────────────────────────

    [Theory]
    [InlineData(  0, 1, 1, 255,   0,   0)]  // red
    [InlineData(120, 1, 1,   0, 255,   0)]  // green
    [InlineData(240, 1, 1,   0,   0, 255)]  // blue
    [InlineData( 60, 1, 1, 255, 255,   0)]  // yellow
    [InlineData(180, 1, 1,   0, 255, 255)]  // cyan
    [InlineData(300, 1, 1, 255,   0, 255)]  // magenta
    public void HsvToRgb_PrimaryColors_Correct(double h, double s, double v, byte er, byte eg, byte eb)
    {
        var (r, g, b) = ColorHelper.HsvToRgb(h, s, v);
        Assert.Equal(er, r);
        Assert.Equal(eg, g);
        Assert.Equal(eb, b);
    }

    [Fact]
    public void HsvToRgb_ZeroSaturation_ProducesGray()
    {
        var (r, g, b) = ColorHelper.HsvToRgb(42, 0, 0.5);
        Assert.Equal(r, g);
        Assert.Equal(g, b);
    }

    [Fact]
    public void HsvToRgb_HueWrapAround_Treated360As0()
    {
        var (r0, g0, b0) = ColorHelper.HsvToRgb(0,   1, 1);
        var (r1, g1, b1) = ColorHelper.HsvToRgb(360, 1, 1);
        Assert.Equal(r0, r1);
        Assert.Equal(g0, g1);
        Assert.Equal(b0, b1);
    }

    // ── RgbToHsv ──────────────────────────────────────────────────────────────

    [Theory]
    [InlineData(255,   0,   0,   0, 1, 1)]  // red
    [InlineData(  0, 255,   0, 120, 1, 1)]  // green
    [InlineData(  0,   0, 255, 240, 1, 1)]  // blue
    public void RgbToHsv_PrimaryColors_Correct(byte r, byte g, byte b, double eh, double es, double ev)
    {
        var (h, s, v) = ColorHelper.RgbToHsv(r, g, b);
        Assert.Equal(eh, h, precision: 1);
        Assert.Equal(es, s, precision: 2);
        Assert.Equal(ev, v, precision: 2);
    }

    [Fact]
    public void RgbToHsv_White_ZeroSaturation()
    {
        var (_, s, v) = ColorHelper.RgbToHsv(255, 255, 255);
        Assert.Equal(0, s, precision: 2);
        Assert.Equal(1, v, precision: 2);
    }

    [Fact]
    public void RoundTrip_RgbHsvRgb_Stable()
    {
        byte r = 200, g = 100, b = 50;
        var (h, s, v) = ColorHelper.RgbToHsv(r, g, b);
        var (r2, g2, b2) = ColorHelper.HsvToRgb(h, s, v);
        Assert.InRange(Math.Abs(r - r2), 0, 1);
        Assert.InRange(Math.Abs(g - g2), 0, 1);
        Assert.InRange(Math.Abs(b - b2), 0, 1);
    }
}
