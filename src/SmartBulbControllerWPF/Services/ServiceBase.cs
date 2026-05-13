using Microsoft.Extensions.Logging;

namespace SmartBulbControllerWPF.Services;

public abstract class ServiceBase
{
    protected readonly ILogger Logger;

    protected ServiceBase(ILogger logger)
    {
        Logger = logger;
    }

    protected async Task<T?> ExecuteAsync<T>(Func<Task<T>> action, string operationName)
    {
        try
        {
            return await action();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error during {Operation}", operationName);
            return default;
        }
    }

    protected async Task ExecuteAsync(Func<Task> action, string operationName)
    {
        try
        {
            await action();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error during {Operation}", operationName);
        }
    }
}
