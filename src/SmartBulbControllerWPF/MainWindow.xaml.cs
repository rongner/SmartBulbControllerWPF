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
}
