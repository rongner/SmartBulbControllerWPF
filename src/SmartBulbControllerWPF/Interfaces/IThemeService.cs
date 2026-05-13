using SmartBulbControllerWPF.Models;

namespace SmartBulbControllerWPF.Interfaces;

public interface IThemeService
{
    void Apply(ThemePreference preference);
    void ApplySaved();
}
