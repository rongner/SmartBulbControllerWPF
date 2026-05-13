using SmartBulbControllerWPF.Models;

namespace SmartBulbControllerWPF.Interfaces;

public interface IScheduleService
{
    IReadOnlyList<ScheduleEntry> Entries { get; }
    void Add(ScheduleEntry entry);
    void Remove(Guid id);
    void SetEnabled(Guid id, bool enabled);
    void Start();
    void Stop();
}
