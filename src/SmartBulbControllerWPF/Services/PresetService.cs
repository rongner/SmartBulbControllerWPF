using System.IO;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using SmartBulbControllerWPF.Interfaces;
using SmartBulbControllerWPF.Models;

namespace SmartBulbControllerWPF.Services;

public class PresetService : ServiceBase, IPresetService
{
    private static readonly string DefaultPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "SmartBulbControllerWPF", "presets.json");

    private readonly string _path;
    private readonly List<Preset> _presets;

    private static readonly Preset[] BuiltIns =
    [
        new() { Name = "Warm White",   IsWhiteMode = true,  Brightness = 80,  ColorTemperature = 10,  IsBuiltIn = true },
        new() { Name = "Cool White",   IsWhiteMode = true,  Brightness = 100, ColorTemperature = 90,  IsBuiltIn = true },
        new() { Name = "Daylight",     IsWhiteMode = true,  Brightness = 100, ColorTemperature = 60,  IsBuiltIn = true },
        new() { Name = "Night Light",  IsWhiteMode = true,  Brightness = 15,  ColorTemperature = 5,   IsBuiltIn = true },
        new() { Name = "Movie Night",  IsWhiteMode = false, Brightness = 30,  R = 180, G = 30,  B = 10, IsBuiltIn = true },
        new() { Name = "Relax",        IsWhiteMode = false, Brightness = 60,  R = 255, G = 120, B = 30, IsBuiltIn = true },
        new() { Name = "Focus",        IsWhiteMode = true,  Brightness = 100, ColorTemperature = 80,  IsBuiltIn = true },
    ];

    public PresetService(ILogger<PresetService> logger) : this(logger, DefaultPath) { }

    internal PresetService(ILogger<PresetService> logger, string path) : base(logger)
    {
        _path    = path;
        _presets = [.. BuiltIns, .. LoadCustom()];
    }

    public IReadOnlyList<Preset> Presets => _presets;

    public void SaveCustom(Preset preset)
    {
        if (preset.IsBuiltIn) throw new InvalidOperationException("Cannot overwrite a built-in preset.");

        var existing = _presets.FindIndex(p => !p.IsBuiltIn && p.Name == preset.Name);
        if (existing >= 0)
            _presets[existing] = preset;
        else
            _presets.Add(preset);

        Persist();
    }

    public void Delete(Preset preset)
    {
        if (preset.IsBuiltIn) throw new InvalidOperationException("Cannot delete a built-in preset.");
        _presets.Remove(preset);
        Persist();
    }

    private IEnumerable<Preset> LoadCustom()
    {
        if (!File.Exists(_path)) return [];
        try
        {
            var json = File.ReadAllText(_path);
            return JsonSerializer.Deserialize<List<Preset>>(json) ?? [];
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Failed to load presets from {Path}", _path);
            return [];
        }
    }

    private void Persist()
    {
        var custom = _presets.Where(p => !p.IsBuiltIn).ToList();
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
            File.WriteAllText(_path, JsonSerializer.Serialize(custom, new JsonSerializerOptions { WriteIndented = true }));
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to save presets to {Path}", _path);
        }
    }
}
