using Microsoft.Extensions.Logging.Abstractions;
using SmartBulbControllerWPF.Models;
using SmartBulbControllerWPF.Services;

namespace SmartBulbControllerWPF.Tests.Services;

public class PresetServiceTests : IDisposable
{
    private readonly string _tempPath;

    public PresetServiceTests() =>
        _tempPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".json");

    public void Dispose()
    {
        if (File.Exists(_tempPath)) File.Delete(_tempPath);
    }

    private PresetService CreateService() =>
        new(NullLogger<PresetService>.Instance, _tempPath);

    [Fact]
    public void NewService_ContainsBuiltInPresets()
    {
        var svc = CreateService();
        Assert.True(svc.Presets.Count >= 7);
        Assert.All(svc.Presets.Where(p => p.IsBuiltIn), p => Assert.True(p.IsBuiltIn));
    }

    [Fact]
    public void SaveCustom_AddsToList()
    {
        var svc    = CreateService();
        var preset = new Preset { Name = "My Custom" };

        svc.SaveCustom(preset);

        Assert.Contains(svc.Presets, p => p.Name == "My Custom" && !p.IsBuiltIn);
    }

    [Fact]
    public void SaveCustom_SameNameOverwrites()
    {
        var svc = CreateService();
        svc.SaveCustom(new Preset { Name = "Dupe", Brightness = 50 });
        svc.SaveCustom(new Preset { Name = "Dupe", Brightness = 80 });

        var matches = svc.Presets.Where(p => p.Name == "Dupe").ToList();
        Assert.Single(matches);
        Assert.Equal(80, matches[0].Brightness);
    }

    [Fact]
    public void SaveCustom_BuiltIn_Throws()
    {
        var svc = CreateService();
        Assert.Throws<InvalidOperationException>(
            () => svc.SaveCustom(new Preset { Name = "Boom", IsBuiltIn = true }));
    }

    [Fact]
    public void Delete_RemovesCustomPreset()
    {
        var svc    = CreateService();
        var preset = new Preset { Name = "ToDelete" };
        svc.SaveCustom(preset);

        var saved = svc.Presets.First(p => p.Name == "ToDelete");
        svc.Delete(saved);

        Assert.DoesNotContain(svc.Presets, p => p.Name == "ToDelete");
    }

    [Fact]
    public void Delete_BuiltIn_Throws()
    {
        var svc     = CreateService();
        var builtIn = svc.Presets.First(p => p.IsBuiltIn);
        Assert.Throws<InvalidOperationException>(() => svc.Delete(builtIn));
    }

    [Fact]
    public void Presets_PersistedAcrossInstances()
    {
        var svc1 = CreateService();
        svc1.SaveCustom(new Preset { Name = "Persistent", Brightness = 42 });

        var svc2   = CreateService();
        var loaded = svc2.Presets.FirstOrDefault(p => p.Name == "Persistent");

        Assert.NotNull(loaded);
        Assert.Equal(42, loaded.Brightness);
    }
}
