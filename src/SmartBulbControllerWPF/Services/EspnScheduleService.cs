using System.Net.Http;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace SmartBulbControllerWPF.Services;

public class EspnScheduleService : ServiceBase
{
    private readonly IHttpClientFactory _httpFactory;

    private const string UrlTemplate =
        "https://site.api.espn.com/apis/site/v2/sports/basketball/nba/teams/{0}/schedule";

    public EspnScheduleService(IHttpClientFactory httpFactory, ILogger<EspnScheduleService> logger)
        : base(logger)
    {
        _httpFactory = httpFactory;
    }

    public async Task<DateTime?> GetNextGameTimeAsync(int teamId, CancellationToken ct = default)
    {
        try
        {
            var url    = string.Format(UrlTemplate, teamId);
            var client = _httpFactory.CreateClient();
            var json   = await client.GetStringAsync(url, ct);
            return ParseNextGame(json);
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Failed to fetch schedule for team {TeamId}", teamId);
            return null;
        }
    }

    internal static DateTime? ParseNextGame(string json)
    {
        using var doc   = JsonDocument.Parse(json);
        var       root  = doc.RootElement;

        if (!root.TryGetProperty("events", out var events)) return null;

        var now = DateTime.UtcNow;

        foreach (var ev in events.EnumerateArray())
        {
            if (!ev.TryGetProperty("date", out var dateProp)) continue;
            if (!DateTime.TryParse(dateProp.GetString(), out var gameTime)) continue;
            gameTime = gameTime.ToUniversalTime();
            if (gameTime > now) return gameTime.ToLocalTime();
        }

        return null;
    }
}
