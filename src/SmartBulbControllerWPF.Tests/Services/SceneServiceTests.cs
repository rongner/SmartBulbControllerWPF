using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using SmartBulbControllerWPF.Interfaces;
using SmartBulbControllerWPF.Models;
using SmartBulbControllerWPF.Services;

namespace SmartBulbControllerWPF.Tests.Services;

public class SceneServiceTests
{
    private readonly Mock<IDeviceService> _deviceSvc = new();
    private readonly Mock<IAudioService>  _audioSvc  = new();

    private SceneService Create() =>
        new(_deviceSvc.Object, _audioSvc.Object, NullLogger<SceneService>.Instance);

    [Fact]
    public void InitialState_IsNone()
    {
        var svc = Create();
        Assert.Equal(SceneType.None, svc.ActiveScene);
    }

    [Fact]
    public void Start_SetsActiveScene()
    {
        var svc = Create();
        _deviceSvc.Setup(d => d.SetColorAsync(It.IsAny<byte>(), It.IsAny<byte>(), It.IsAny<byte>(),
            It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        svc.Start(SceneType.ColorCycle, 255, 0, 0, 9999);

        Assert.Equal(SceneType.ColorCycle, svc.ActiveScene);
    }

    [Fact]
    public void Stop_ResetsActiveSceneToNone()
    {
        var svc = Create();
        _deviceSvc.Setup(d => d.SetColorAsync(It.IsAny<byte>(), It.IsAny<byte>(), It.IsAny<byte>(),
            It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        svc.Start(SceneType.Pulse, 0, 0, 255, 9999);
        svc.Stop();

        Assert.Equal(SceneType.None, svc.ActiveScene);
    }

    [Fact]
    public void Stop_WhenNothingRunning_DoesNotThrow()
    {
        var svc = Create();
        var ex  = Record.Exception(() => svc.Stop());
        Assert.Null(ex);
    }

    [Fact]
    public void Start_None_DoesNotSetActiveScene()
    {
        var svc = Create();
        svc.Start(SceneType.None, 255, 255, 255, 100);
        Assert.Equal(SceneType.None, svc.ActiveScene);
    }

    [Fact]
    public void Start_NewScene_StopsPreviousScene()
    {
        var svc = Create();
        _deviceSvc.Setup(d => d.SetColorAsync(It.IsAny<byte>(), It.IsAny<byte>(), It.IsAny<byte>(),
            It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _deviceSvc.Setup(d => d.SetBrightnessAsync(It.IsAny<int>(),
            It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        svc.Start(SceneType.ColorCycle, 0, 255, 0, 9999);
        svc.Start(SceneType.Strobe, 255, 0, 0, 9999);

        Assert.Equal(SceneType.Strobe, svc.ActiveScene);
    }

    [Fact]
    public async Task ColorCycle_CallsSetColorAsync()
    {
        var svc = Create();
        var called = new TaskCompletionSource<bool>();

        _deviceSvc.Setup(d => d.SetColorAsync(It.IsAny<byte>(), It.IsAny<byte>(), It.IsAny<byte>(),
            It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask)
            .Callback(() => called.TrySetResult(true));

        svc.Start(SceneType.ColorCycle, 255, 255, 255, 30);
        await Task.WhenAny(called.Task, Task.Delay(2000));
        svc.Stop();

        Assert.True(called.Task.IsCompletedSuccessfully);
    }
}
