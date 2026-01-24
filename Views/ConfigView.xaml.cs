using System.Windows.Controls;
using TimeTracker.ViewModels;

namespace TimeTracker.Views
{
    public partial class ConfigView : UserControl
    {
        public ConfigView()
        {
            InitializeComponent();
        }

        public void SetViewModel(MainViewModel mainVm)
        {
            DataContext = new ConfigViewModel(mainVm);
        }
    }
}

