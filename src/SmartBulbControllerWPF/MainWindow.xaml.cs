using System.ComponentModel;
using MahApps.Metro.Controls;
using SmartBulbControllerWPF.ViewModels;

namespace SmartBulbControllerWPF;

public partial class MainWindow : MetroWindow
{
    public MainWindow(MainViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }

    protected override async void OnContentRendered(EventArgs e)
    {
        base.OnContentRendered(e);
        if (DataContext is MainViewModel vm)
            await vm.AutoReconnectAsync();
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        if (!App.IsExiting)
        {
            e.Cancel = true;
            Hide();
            return;
        }
        base.OnClosing(e);
    }
}
