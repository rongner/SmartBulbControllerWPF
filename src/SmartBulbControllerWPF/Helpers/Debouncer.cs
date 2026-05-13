namespace SmartBulbControllerWPF.Helpers;

internal sealed class Debouncer : IDisposable
{
    private readonly int _delayMs;
    private CancellationTokenSource _cts = new();

    public Debouncer(int delayMs) => _delayMs = delayMs;

    public void Schedule(Func<Task> action)
    {
        _cts.Cancel();
        _cts = new CancellationTokenSource();
        var token = _cts.Token;
        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(_delayMs, token);
                await action();
            }
            catch (OperationCanceledException) { }
        });
    }

    public void Dispose() => _cts.Dispose();
}
