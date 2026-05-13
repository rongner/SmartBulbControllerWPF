namespace SmartBulbControllerWPF.Models;

public record DiscoveredDevice(string Ip, string DeviceId, string Version)
{
    public string? FriendlyName { get; set; }
    public string DisplayName   => FriendlyName ?? Ip;
}
