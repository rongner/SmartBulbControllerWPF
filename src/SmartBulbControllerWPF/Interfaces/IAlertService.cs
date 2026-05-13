namespace SmartBulbControllerWPF.Interfaces;

public interface IAlertService
{
    bool IsRunning { get; }
    void Start();
    void Stop();
}
