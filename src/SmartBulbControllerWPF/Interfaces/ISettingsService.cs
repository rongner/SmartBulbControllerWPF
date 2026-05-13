using SmartBulbControllerWPF.Models;

namespace SmartBulbControllerWPF.Interfaces;

public interface ISettingsService
{
    AppSettings Current { get; }
    void Save();
    string? GetLocalKey();
    void SetLocalKey(string localKey);
}
