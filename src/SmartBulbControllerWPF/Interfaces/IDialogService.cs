using SmartBulbControllerWPF.Models;

namespace SmartBulbControllerWPF.Interfaces;

public interface IDialogService
{
    Task<bool> ShowConfirmAsync(string title, string message);
    Task ShowAlertAsync(string title, string message);
    Task ShowAboutAsync();
    Task<ConnectDialogResult?> ShowConnectDialogAsync(string? initialIp = null, string? initialDeviceId = null);
}
