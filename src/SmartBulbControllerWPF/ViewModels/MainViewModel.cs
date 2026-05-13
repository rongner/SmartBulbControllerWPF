using Microsoft.Extensions.Logging;

namespace SmartBulbControllerWPF.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    public MainViewModel(ILogger<MainViewModel> logger) : base(logger)
    {
    }
}
