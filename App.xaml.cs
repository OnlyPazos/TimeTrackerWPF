using System.Windows;
using TimeTracker.Services;
using TimeTracker.ViewModels;
using TimeTracker.Views;

namespace TimeTracker
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            IAppSettings settings = new AppSettings();
            ITaskPersistenceService persistence = new JsonTaskPersistenceService();
            IDialogService dialogService = new DialogService();

            var mainViewModel = new MainViewModel(dialogService, persistence, settings);

            var mainWindow = new MainWindow
            {
                DataContext = mainViewModel
            };

            mainWindow.Show();
        }
    }
}
