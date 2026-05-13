using Microsoft.Extensions.Logging;
using NAudio.Wave;
using SmartBulbControllerWPF.Interfaces;

namespace SmartBulbControllerWPF.Services;

public class AudioService : ServiceBase, IAudioService, IDisposable
{
    private WaveInEvent? _waveIn;
    private volatile float _volume;

    public bool  IsCapturing   { get; private set; }
    public float CurrentVolume => _volume;

    public AudioService(ILogger<AudioService> logger) : base(logger) { }

    public void Start()
    {
        if (IsCapturing) return;
        try
        {
            _waveIn = new WaveInEvent { WaveFormat = new WaveFormat(44100, 1) };
            _waveIn.DataAvailable += OnDataAvailable;
            _waveIn.StartRecording();
            IsCapturing = true;
            Logger.LogInformation("Audio capture started");
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Failed to start audio capture — no microphone?");
            _waveIn?.Dispose();
            _waveIn = null;
        }
    }

    public void Stop()
    {
        if (!IsCapturing) return;
        _waveIn?.StopRecording();
        _waveIn?.Dispose();
        _waveIn     = null;
        _volume     = 0;
        IsCapturing = false;
        Logger.LogInformation("Audio capture stopped");
    }

    public void Dispose() => Stop();

    private void OnDataAvailable(object? sender, WaveInEventArgs e)
    {
        float peak = 0;
        for (int i = 0; i < e.BytesRecorded - 1; i += 2)
        {
            float sample = Math.Abs(BitConverter.ToInt16(e.Buffer, i) / 32768f);
            if (sample > peak) peak = sample;
        }
        // Exponential smoothing — fast attack, slow decay
        _volume = peak > _volume
            ? peak * 0.7f + _volume * 0.3f   // attack
            : peak * 0.1f + _volume * 0.9f;  // decay
    }
}
