namespace TimeTracker.Services
{
    public interface IDialogService
    {
        void ShowMessage(string message, string title = "Info");
    }
}
