namespace SmartBulbControllerWPF.Models;

public record DiscoveredDevice(string Ip, string DeviceId, string Version)
{
    public string? FriendlyName { get; set; }
    public bool    IsGrouped    { get; set; }
    public string  DisplayName  => FriendlyName ?? Ip;
}
