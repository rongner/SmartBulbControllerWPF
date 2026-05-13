using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace SmartBulbControllerWPF.ViewModels;

public partial class ConnectDialogViewModel : ObservableObject
{
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ConnectCommand))]
    private string _ip = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ConnectCommand))]
    private string _deviceId = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ConnectCommand))]
    private string _localKey = string.Empty;

    public bool Confirmed { get; private set; }

    public event Action? CloseRequested;

    [RelayCommand(CanExecute = nameof(CanConnect))]
    private void Connect()
    {
        Confirmed = true;
        CloseRequested?.Invoke();
    }

    [RelayCommand]
    private void Cancel() => CloseRequested?.Invoke();

    private bool CanConnect() =>
        !string.IsNullOrWhiteSpace(Ip) &&
        !string.IsNullOrWhiteSpace(DeviceId) &&
        !string.IsNullOrWhiteSpace(LocalKey);
}
