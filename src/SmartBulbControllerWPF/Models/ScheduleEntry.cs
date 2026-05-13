namespace SmartBulbControllerWPF.Models;

public class ScheduleEntry
{
    public Guid     Id        { get; set; } = Guid.NewGuid();
    public TimeSpan Time      { get; set; }
    public bool     TurnOn    { get; set; } = true;
    public bool     IsEnabled { get; set; } = true;
    public bool     IsDaily   { get; set; } = true;
    public DateTime? LastFired { get; set; }

    public string DisplayText =>
        $"{DateTime.Today.Add(Time):h:mm tt} — {(TurnOn ? "Turn On" : "Turn Off")}" +
        (IsDaily ? "" : " (once)");
}
