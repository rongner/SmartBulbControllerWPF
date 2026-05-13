namespace SmartBulbControllerWPF.Interfaces;

public interface IAudioService
{
    bool  IsCapturing   { get; }
    float CurrentVolume { get; }  // 0–1, smoothed peak amplitude
    void  Start();
    void  Stop();
}
