using SmartBulbControllerWPF.ViewModels;

namespace SmartBulbControllerWPF.Tests.ViewModels;

public class ColorControlTests
{
    // ── TryParseHex ───────────────────────────────────────────────────────────

    [Theory]
    [InlineData("#FF0000", 255, 0, 0)]
    [InlineData("#00FF00", 0, 255, 0)]
    [InlineData("#0000FF", 0, 0, 255)]
    [InlineData("#FFFFFF", 255, 255, 255)]
    [InlineData("#000000", 0, 0, 0)]
    [InlineData("#1A2B3C", 0x1A, 0x2B, 0x3C)]
    [InlineData("FF8800",  255, 136, 0)]   // no hash
    public void TryParseHex_ValidInput_ParsesCorrectly(string input, byte r, byte g, byte b)
    {
        Assert.True(MainViewModel.TryParseHex(input, out var pr, out var pg, out var pb));
        Assert.Equal(r, pr);
        Assert.Equal(g, pg);
        Assert.Equal(b, pb);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("#FFF")]       // too short
    [InlineData("#GGGGGG")]    // invalid hex chars
    [InlineData("#1234567")]   // too long
    public void TryParseHex_InvalidInput_ReturnsFalse(string? input)
    {
        Assert.False(MainViewModel.TryParseHex(input, out _, out _, out _));
    }

    // ── RGB → Hex sync ────────────────────────────────────────────────────────

    [Fact]
    public void SettingRgb_UpdatesHexColor()
    {
        var vm = ColorControlTestHelper.CreateVm();

        vm.ColorRed   = 0xFF;
        vm.ColorGreen = 0x80;
        vm.ColorBlue  = 0x10;

        Assert.Equal("#FF8010", vm.HexColor);
    }

    [Fact]
    public void SettingHex_UpdatesRgbSliders()
    {
        var vm = ColorControlTestHelper.CreateVm();

        vm.HexColor = "#3A7F1E";

        Assert.Equal(0x3A, vm.ColorRed);
        Assert.Equal(0x7F, vm.ColorGreen);
        Assert.Equal(0x1E, vm.ColorBlue);
    }

    [Fact]
    public void SettingInvalidHex_DoesNotUpdateRgb()
    {
        var vm = ColorControlTestHelper.CreateVm();
        vm.ColorRed   = 10;
        vm.ColorGreen = 20;
        vm.ColorBlue  = 30;

        vm.HexColor = "#ZZZ";  // invalid — no change

        Assert.Equal(10, vm.ColorRed);
        Assert.Equal(20, vm.ColorGreen);
        Assert.Equal(30, vm.ColorBlue);
    }

    [Fact]
    public void SettingHex_DoesNotCauseInfiniteLoop()
    {
        var vm = ColorControlTestHelper.CreateVm();
        int changes = 0;
        vm.PropertyChanged += (_, e) => { if (e.PropertyName == nameof(vm.HexColor)) changes++; };

        vm.HexColor = "#123456";

        Assert.Equal(1, changes);
    }

    [Fact]
    public void SettingRgb_DoesNotCauseInfiniteLoop()
    {
        var vm = ColorControlTestHelper.CreateVm();
        int changes = 0;
        vm.PropertyChanged += (_, e) => { if (e.PropertyName == nameof(vm.HexColor)) changes++; };

        vm.ColorRed = 100;

        Assert.Equal(1, changes);
    }
}
