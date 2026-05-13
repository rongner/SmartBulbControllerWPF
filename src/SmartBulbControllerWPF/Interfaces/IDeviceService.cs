using SmartBulbControllerWPF.Models;

namespace SmartBulbControllerWPF.Interfaces;

public interface IDeviceService
{
    bool IsConnected { get; }

    Task<IEnumerable<DiscoveredDevice>> ScanAsync(int scanSeconds = 5, CancellationToken ct = default);
    Task ConnectAsync(string ip, string deviceId, string localKey, CancellationToken ct = default);
    void Disconnect();

    Task<DeviceState> GetStateAsync(CancellationToken ct = default);
    Task SetPowerAsync(bool on, CancellationToken ct = default);
    Task SetColorAsync(byte r, byte g, byte b, CancellationToken ct = default);
    Task SetBrightnessAsync(int percent, CancellationToken ct = default);
    Task SetColorTemperatureAsync(int percent, CancellationToken ct = default);

    // Group (secondary bulbs that mirror every command)
    IReadOnlyList<string> GroupMemberIps { get; }
    Task AddToGroupAsync(string ip, string deviceId, string localKey, CancellationToken ct = default);
    void RemoveFromGroup(string ip);
    void ClearGroup();
}
