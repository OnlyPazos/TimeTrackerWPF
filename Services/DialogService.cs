using System.Windows;

namespace TimeTracker.Services
{
    public class DialogService : IDialogService
    {
        public void ShowMessage(string message, string title = "Info")
        {
            MessageBox.Show(message, title);
        }
    }
}
