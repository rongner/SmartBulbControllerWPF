using System.Windows;
using System.Windows.Controls;
using MahApps.Metro.Controls;
using SmartBulbControllerWPF.ViewModels;

namespace SmartBulbControllerWPF.Views;

public partial class ConnectDialog : MetroWindow
{
    public ConnectDialog(ConnectDialogViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        viewModel.CloseRequested += Close;
    }

    private void LocalKeyBox_PasswordChanged(object sender, RoutedEventArgs e)
    {
        if (DataContext is ConnectDialogViewModel vm)
            vm.LocalKey = ((PasswordBox)sender).Password;
    }
}
