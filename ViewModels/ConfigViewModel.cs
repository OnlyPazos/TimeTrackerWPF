using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Input;
using TimeTracker.Commands;
using TimeTracker.Models;
using TimeTracker.ViewModels;

public class ConfigViewModel : INotifyPropertyChanged
{
    private readonly MainViewModel _mainVm;

    public ConfigViewModel(MainViewModel mainVm)
    {
        _mainVm = mainVm;
        Tasks = _mainVm.Tasks;

        AddTaskCommand = new RelayCommand(() =>
        {
            var task = new TaskViewModel(
                new TaskModel { Name = "Nueva tarea", ColorHex = "#888888" },
                _mainVm,
                t => { }
            );

            Tasks.Add(task);
            SelectedTask = task;
        });

        RemoveTaskCommand = new RelayCommand(() =>
        {
            if (SelectedTask != null)
            {
                Tasks.Remove(SelectedTask);
                SelectedTask = null;
            }
        });
    }

    public ObservableCollection<TaskViewModel> Tasks { get; }

    private TaskViewModel _selectedTask;
    public TaskViewModel SelectedTask
    {
        get => _selectedTask;
        set { _selectedTask = value; OnPropertyChanged(nameof(SelectedTask)); }
    }

    public int MaxColumns
    {
        get => _mainVm.MaxColumns;
        set { _mainVm.MaxColumns = value; OnPropertyChanged(nameof(MaxColumns)); }
    }

    public ICommand AddTaskCommand { get; }
    public ICommand RemoveTaskCommand { get; }

    public event PropertyChangedEventHandler PropertyChanged;
    protected void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
