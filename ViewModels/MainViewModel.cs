using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using TimeTracker.Commands;
using TimeTracker.Models;
using TimeTracker.Providers;
using TimeTracker.Services;

namespace TimeTracker.ViewModels
{
    public class MainViewModel : BaseViewModel, ITotalTimeProvider
    {
        private readonly IAppSettings _settings;
        private readonly ITaskPersistenceService _persistence;
        private readonly IDialogService _dialogService;
        private readonly AutoSaveService _autoSave;
        private Dictionary<Guid, TaskViewModel> _taskById;
        public event Action TimelineStructureChanged;

        Brush IdleBrush = new SolidColorBrush(Color.FromRgb(200, 200, 200));
        public TimeSpan TimelineTotal { get; } = TimeSpan.FromHours(18);
        public DateTime TimelineStart { get; } = DateTime.Today.AddHours(6);

        public ObservableCollection<IntervalViewModel> AllIntervals { get; } = new ObservableCollection<IntervalViewModel>();

        public ObservableCollection<TaskViewModel> Tasks { get; }
        public TimeSpan TotalTime => Tasks.Aggregate(TimeSpan.Zero, (acc, t) => acc + t.Total);

        private TaskViewModel _selectedTask;
        public TaskViewModel SelectedTask
        {
            get => _selectedTask;
            set { _selectedTask = value; OnPropertyChanged(); }
        }

        public ICommand AddTaskCommand { get; }

        public MainViewModel(IDialogService dialogService, ITaskPersistenceService persistenceService, IAppSettings settings)
        {
            _dialogService = dialogService;
            _persistence = persistenceService;
            _settings = settings;

            var models = _persistence.Load(_settings.TasksFilePath)?.ToList();

            if (models == null || models.Count == 0)
            {
                models = new List<TaskModel>()
                {
                    new TaskModel { Name = "Task 1", ColorHex = "#787878" },
                    new TaskModel { Name = "Task 2", ColorHex = "#FD5A70" },
                    new TaskModel { Name = "Task 3", ColorHex = "#FED068" },
                    new TaskModel { Name = "Task 4", ColorHex = "#00BFA0" },
                    new TaskModel { Name = "Task 5", ColorHex = "#0580B0" },
                };
            }

            Tasks = new ObservableCollection<TaskViewModel>(
                models.Select(m => new TaskViewModel(m, this, SelectTask))
            );

            BuildTaskIndex();

            AddTaskCommand = new RelayCommand(AddTask);

            LoadIntervals(models);

            Tasks.CollectionChanged += (_, e) =>
            {
                if (e.NewItems != null)
                {
                    foreach (TaskViewModel t in e.NewItems)
                        _taskById[t.Id] = t;
                }

                if (e.OldItems != null)
                {
                    foreach (TaskViewModel t in e.OldItems)
                        _taskById.Remove(t.Id);
                }
            };

            AllIntervals.CollectionChanged += (_, __) =>
            {
                TimelineStructureChanged?.Invoke();
            };

            CompositionTarget.Rendering += OnRendering;

            _autoSave = new AutoSaveService(
                _persistence,
                settings,
                () => Tasks.Select(t => t.ToModel())
            );
        }

        private void OnRendering(object sender, EventArgs e)
        {
            if (!Tasks.Any(t => t.IsRunning))
                return;

            RefreshTotalTime();

            foreach (var task in Tasks)
            {
                task.RefreshTotal();
                task.RefreshIntervals();
            }
        }

        private void AddTask()
        {
            var newTask = new TaskModel { Name = "Nueva Tarea" };
            Tasks.Add(new TaskViewModel(newTask, this, SelectTask));
        }

        public void SelectTask(TaskViewModel task)
        {
            if (SelectedTask != null)
            {
                if (SelectedTask.StopCommand.CanExecute(null))
                    SelectedTask.StopCommand.Execute(null);
            }

            if (SelectedTask == task)
            {
                SelectedTask = null;
                return;
            }

            SelectedTask = task;

            if (task.StartCommand.CanExecute(null))
                task.StartCommand.Execute(null);
        }

        public void RefreshTotalTime()
        {
            OnPropertyChanged(nameof(TotalTime));
        }

        public void LoadIntervals(List<TaskModel> models)
        {
            AllIntervals.Clear();

            foreach (var model in models)
            {
                foreach (var interval in model.Intervals)
                {
                    AllIntervals.Add(new IntervalViewModel() {
                        Interval = interval,
                        Color = new SolidColorBrush((Color)ColorConverter.ConvertFromString(model.ColorHex))
                    });
                }
            }
        }

        private void BuildTaskIndex()
        {
            _taskById = Tasks.ToDictionary(t => t.Id);
        }

        private Brush CreateDiagonalHatchBrush(Color color, double spacing = 6)
        {
            var geometryGroup = new GeometryGroup();

            // Creamos líneas diagonales que atraviesan el tile
            for (double i = -spacing * 2; i < spacing * 4; i += spacing)
            {
                var line = new LineGeometry(new Point(i, 0), new Point(i - spacing, spacing));
                geometryGroup.Children.Add(line);
            }

            var drawingBrush = new DrawingBrush
            {
                TileMode = TileMode.Tile,
                Viewport = new Rect(0, 0, spacing, spacing),
                ViewportUnits = BrushMappingMode.Absolute,
                Stretch = Stretch.Fill,
                Drawing = new GeometryDrawing
                {
                    Pen = new Pen(new SolidColorBrush(color), 1),
                    Geometry = geometryGroup
                },
                Transform = new RotateTransform(45, 0.5, 0.5),
            };

            return drawingBrush;
        }

        public IEnumerable<TimelineBlock> BuildTimelineBlocks()
        {
            var blocks = new List<TimelineBlock>();

            var start = TimelineStart;
            var end = TimelineStart + TimelineTotal;

            var intervals = AllIntervals
                .Select(i => i.Interval)
                .Where(i =>
                    i.Start < end &&
                    i.Start + i.Duration > start
                )
                .OrderBy(i => i.Start)
                .ToList();

            DateTimeOffset cursor = start;

            foreach (var interval in intervals)
            {
                // Ignorar intervalos fuera de rango
                if (interval.Start >= end)
                    break;

                // Intervalo empieza antes del cursor
                var intervalStart = interval.Start < start ? start : interval.Start;

                // 1️⃣ Hueco (idle)
                if (intervalStart > cursor)
                {
                    blocks.Add(new TimelineBlock
                    {
                        Start = cursor,
                        Duration = intervalStart - cursor,
                        Color = CreateDiagonalHatchBrush(Color.FromRgb(200, 200, 200)),
                        IsIdle = true,
                        Label = $"Idle\n{cursor:HH:mm} - {intervalStart:HH:mm}"
                    });
                }

                // 2️⃣ Intervalo real
                var intervalEnd = intervalStart + interval.Duration;
                if (intervalEnd > end)
                    intervalEnd = end;

                var taskVm = _taskById[interval.TaskId];
                blocks.Add(new TimelineBlock
                {
                    Start = intervalStart,
                    Duration = intervalEnd - intervalStart,
                    Color = taskVm.Color,
                    IsIdle = false,
                    Label = $"{taskVm.Name}\n{intervalStart:HH:mm} - {intervalEnd:HH:mm}\n{intervalEnd - intervalStart:hh\\:mm}"
                });

                cursor = intervalEnd;
            }

            // 3️⃣ Idle final
            if (cursor < end)
            {
                blocks.Add(new TimelineBlock
                {
                    Start = cursor,
                    Duration = end - cursor,
                    Color = IdleBrush,
                    IsIdle = true,
                    Label = $"Idle\n{cursor:HH:mm} - {cursor:HH:mm}"
                });
            }

            return blocks;
        }
    }
}
