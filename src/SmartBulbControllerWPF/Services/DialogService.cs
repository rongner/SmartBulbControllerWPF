using System.Reflection;
using System.Windows;
using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;
using Microsoft.Extensions.Logging;
using SmartBulbControllerWPF.Interfaces;
using SmartBulbControllerWPF.Models;
using SmartBulbControllerWPF.ViewModels;
using SmartBulbControllerWPF.Views;

namespace SmartBulbControllerWPF.Services;

public class DialogService : ServiceBase, IDialogService
{
    public DialogService(ILogger<DialogService> logger) : base(logger) { }

    public async Task<bool> ShowConfirmAsync(string title, string message)
    {
        var window = GetMainWindow();
        var result = await window.ShowMessageAsync(title, message,
            MessageDialogStyle.AffirmativeAndNegative,
            new MetroDialogSettings { AffirmativeButtonText = "Yes", NegativeButtonText = "No" });
        return result == MessageDialogResult.Affirmative;
    }

    public async Task ShowAlertAsync(string title, string message)
    {
        var window = GetMainWindow();
        await window.ShowMessageAsync(title, message);
    }

    public async Task ShowAboutAsync()
    {
        var version = Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "1.0.0";
        await ShowAlertAsync("Smart Bulb Controller",
            $"Version {version}\n\nControls DAYBETTER RGBCW bulbs over local LAN.\ngithub.com/rongner/SmartBulbControllerWPF");
    }

    public async Task<string?> ShowInputAsync(string title, string message)
    {
        var window = GetMainWindow();
        return await window.ShowInputAsync(title, message);
    }

    public Task<ConnectDialogResult?> ShowConnectDialogAsync(string? initialIp = null, string? initialDeviceId = null)
    {
        var vm = new ConnectDialogViewModel
        {
            Ip       = initialIp       ?? string.Empty,
            DeviceId = initialDeviceId ?? string.Empty,
        };

        var dialog = new ConnectDialog(vm) { Owner = GetMainWindow() };
        dialog.ShowDialog();

        ConnectDialogResult? result = vm.Confirmed
            ? new ConnectDialogResult(vm.Ip, vm.DeviceId, vm.LocalKey)
            : null;

        return Task.FromResult(result);
    }

    private static MetroWindow GetMainWindow() =>
        (MetroWindow)Application.Current.MainWindow;
}
