namespace SmartBulbControllerWPF.Interfaces;

public interface IAlertService
{
    bool IsRunning { get; }
    DateTime? NextGameTime { get; }
    event Action? NextGameUpdated;
    void Start();
    void Stop();
}
