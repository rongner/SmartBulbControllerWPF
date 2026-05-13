using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;

namespace SmartBulbControllerWPF.ViewModels;

public abstract partial class ViewModelBase : ObservableObject
{
    protected readonly ILogger Logger;

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private string? _errorMessage;

    protected ViewModelBase(ILogger logger)
    {
        Logger = logger;
    }

    protected void SetError(string message)
    {
        ErrorMessage = message;
        Logger.LogError(message);
    }

    protected void ClearError() => ErrorMessage = null;

    protected async Task RunBusyAsync(Func<Task> action)
    {
        IsBusy = true;
        ClearError();
        try
        {
            await action();
        }
        catch (Exception ex)
        {
            SetError(ex.Message);
        }
        finally
        {
            IsBusy = false;
        }
    }
}
