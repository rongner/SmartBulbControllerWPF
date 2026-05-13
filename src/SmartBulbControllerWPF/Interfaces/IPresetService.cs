using SmartBulbControllerWPF.Models;

namespace SmartBulbControllerWPF.Interfaces;

public interface IPresetService
{
    IReadOnlyList<Preset> Presets { get; }
    void SaveCustom(Preset preset);
    void Delete(Preset preset);
}
