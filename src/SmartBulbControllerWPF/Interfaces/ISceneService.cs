using SmartBulbControllerWPF.Models;

namespace SmartBulbControllerWPF.Interfaces;

public interface ISceneService
{
    SceneType ActiveScene { get; }
    void Start(SceneType scene, byte r, byte g, byte b, int stepMs);
    void Stop();
}
