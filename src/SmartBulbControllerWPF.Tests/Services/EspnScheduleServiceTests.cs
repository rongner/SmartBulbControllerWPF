using SmartBulbControllerWPF.Services;

namespace SmartBulbControllerWPF.Tests.Services;

public class EspnScheduleServiceTests
{
    private static string MakeJson(params string[] isoDates)
    {
        var events = string.Join(",", isoDates.Select(d => $"{{\"date\":\"{d}\"}}"));
        return $"{{\"events\":[{events}]}}";
    }

    [Fact]
    public void ParseNextGame_FutureGame_ReturnsFutureTime()
    {
        var future = DateTime.UtcNow.AddHours(3).ToString("o");
        var json   = MakeJson(future);

        var result = EspnScheduleService.ParseNextGame(json);

        Assert.NotNull(result);
        Assert.True(result!.Value > DateTime.Now);
    }

    [Fact]
    public void ParseNextGame_AllPastGames_ReturnsNull()
    {
        var past1 = DateTime.UtcNow.AddDays(-2).ToString("o");
        var past2 = DateTime.UtcNow.AddDays(-1).ToString("o");
        var json  = MakeJson(past1, past2);

        var result = EspnScheduleService.ParseNextGame(json);

        Assert.Null(result);
    }

    [Fact]
    public void ParseNextGame_MixedGames_ReturnsNextFuture()
    {
        var past   = DateTime.UtcNow.AddDays(-1).ToString("o");
        var near   = DateTime.UtcNow.AddHours(2).ToString("o");
        var far    = DateTime.UtcNow.AddDays(3).ToString("o");
        var json   = MakeJson(past, near, far);

        var result = EspnScheduleService.ParseNextGame(json);

        Assert.NotNull(result);
        // Should be near, not far (first future event in list)
        Assert.True(result!.Value < DateTime.Now.AddHours(3));
    }

    [Fact]
    public void ParseNextGame_EmptyEvents_ReturnsNull()
    {
        var result = EspnScheduleService.ParseNextGame("{\"events\":[]}");
        Assert.Null(result);
    }

    [Fact]
    public void ParseNextGame_NoEventsKey_ReturnsNull()
    {
        var result = EspnScheduleService.ParseNextGame("{\"foo\":\"bar\"}");
        Assert.Null(result);
    }

    [Fact]
    public void ParseNextGame_InvalidDateEntry_SkipsAndContinues()
    {
        var future = DateTime.UtcNow.AddHours(1).ToString("o");
        var json   = $"{{\"events\":[{{\"date\":\"not-a-date\"}},{{\"date\":\"{future}\"}}]}}";

        var result = EspnScheduleService.ParseNextGame(json);

        Assert.NotNull(result);
    }
}
