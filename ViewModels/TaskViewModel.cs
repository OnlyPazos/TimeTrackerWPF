using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Windows.Input;
using System.Windows.Media;
using TimeTracker.Commands;
using TimeTracker.Models;
using TimeTracker.Providers;

namespace TimeTracker.ViewModels
{
    public class TaskViewModel : BaseViewModel
    {
        private readonly TaskModel _model;
        private readonly Stopwatch _stopwatch = new Stopwatch();
        private bool _isRunning;
        private readonly ITotalTimeProvider _totalTimeProvider;

        public ObservableCollection<TimeInterval> Intervals { get; } = new ObservableCollection<TimeInterval>();

        public Guid Id { get => _model.Id; }

        public string Name
        {
            get => _model.Name;
            set { if (_model.Name != value) _model.Name = value; OnPropertyChanged(); }
        }

        public TimeSpan DailyTotal
        {
            get
            {
                var dayStart = DateTime.Today;
                var dayEnd = dayStart.AddDays(1);

                return Intervals.Aggregate(TimeSpan.Zero, (sum, interval) =>
                {
                    var intervalStart = interval.Start < dayStart
                        ? dayStart
                        : interval.Start;

                    var intervalEnd = interval.Start + interval.Duration;
                    if (intervalEnd > dayEnd)
                        intervalEnd = dayEnd;

                    if (intervalEnd <= intervalStart)
                        return sum;

                    return sum + (intervalEnd - intervalStart);
                });
            }
        }
        public TimeSpan Total => Intervals.Aggregate(TimeSpan.Zero, (sum, i) => sum + i.Duration);

        public double DailyProgress
        {
            get
            {
                if (!(_totalTimeProvider is MainViewModel mainVm))
                    return 0;

                var dayTotal = mainVm.DailyTotal;
                if (dayTotal.TotalMilliseconds <= 0)
                    return 0;

                return DailyTotal.TotalMilliseconds / dayTotal.TotalMilliseconds;
            }
        }

        public double Progress
        {
            get
            {
                if (_totalTimeProvider == null) return 0;

                var total = _totalTimeProvider.TotalTime;
                if (total.TotalMilliseconds <= 0) return 0;

                return Total.TotalMilliseconds / total.TotalMilliseconds;
            }
        }

        public bool IsRunning
        {
            get => _isRunning;
            set { if (_isRunning != value) _isRunning = value; OnPropertyChanged(); ((RelayCommand)StartCommand).RaiseCanExecuteChanged(); ((RelayCommand)StopCommand).RaiseCanExecuteChanged(); }
        }

        public string ColorHex { get => _model.ColorHex; set { if (_model.ColorHex != value) { _model.ColorHex = value; OnPropertyChanged(nameof(ColorHex)); OnPropertyChanged(nameof(Color)); }}}
        public Brush Color { get { try { return (Brush)new BrushConverter().ConvertFromString(_model.ColorHex) ?? Brushes.Gray; } catch { return Brushes.Gray; }}}

        public ICommand StartCommand { get; }
        public ICommand StopCommand { get; }
        public ICommand SelectCommand { get; }

        public TaskViewModel(TaskModel model, ITotalTimeProvider totalTimeProvider, Action<TaskViewModel> onSelect)
        {
            _model = model ?? throw new ArgumentNullException(nameof(model));
            _totalTimeProvider = totalTimeProvider;

            foreach (var interval in model.Intervals)
                Intervals.Add(interval);

            RefreshTotal();

            SelectCommand = new RelayCommand(() => onSelect(this));
            StartCommand = new RelayCommand(Start, () => !IsRunning);
            StopCommand = new RelayCommand(Stop, () => IsRunning);
        }

        public void Start()
        {
            if (IsRunning) return;

            var interval = new TimeInterval
            {
                TaskId = Id,
                Start = DateTimeOffset.Now,
                Duration = TimeSpan.Zero,
                IsFinalized = false
            };

            Intervals.Add(interval);

            if (_totalTimeProvider is MainViewModel mainVm)
            {
                mainVm.AllIntervals.Add(new IntervalViewModel
                {
                    Interval = interval,
                    Color = Color
                });
            }

            _stopwatch.Restart();
            IsRunning = true;
        }

        public void Stop()
        {
            if (!IsRunning) return;

            _stopwatch.Stop();

            var interval = Intervals.Last();
            interval.Duration = DateTimeOffset.Now - interval.Start;
            interval.IsFinalized = true;

            _stopwatch.Reset();
            IsRunning = false;

            RefreshTotal();
        }

        public bool ResumeFromInterval(TimeInterval interval)
        {
            if (interval.IsFinalized) return false;

            var startDay = interval.Start.Date;
            var today = DateTimeOffset.Now.Date;

            // Caso 1: mismo día → Simplemente reanudar
            if (startDay == today)
            {
                _stopwatch.Restart();
                IsRunning = true;
                return true;
            }

            // Caso 2: cruzó uno o más días → Cerramos intervalo anterior a las 23:59:59
            var endOfStartDay = startDay.AddDays(1).AddTicks(-1);
            interval.Duration = endOfStartDay - interval.Start;
            interval.IsFinalized = true;

            return false;
        }

        public void RefreshTotal()
        {
            OnPropertyChanged(nameof(DailyTotal));
            OnPropertyChanged(nameof(DailyProgress));

            OnPropertyChanged(nameof(Total));
            OnPropertyChanged(nameof(Progress));
        }

        public void RefreshIntervals()
        {
            if (!IsRunning) return;

            var interval = Intervals.Last();
            interval.Duration = (interval.IsFinalized ? interval.Duration : DateTimeOffset.Now - interval.Start);

            //Console.WriteLine(string.Join(", ", Intervals.Select(i => $"[{i.Start:HH:mm:ss} - {i.Duration}]")));
        }

        public TaskModel ToModel()
        {
            return new TaskModel
            {
                Id = Id,
                Name = Name,
                ColorHex = _model.ColorHex,
                Intervals = Intervals.ToList()
            };
        }
    }
    public class TimeInterval : INotifyPropertyChanged
    {
        private TimeSpan _duration;

        public Guid TaskId { get; set; }
        public DateTimeOffset Start { get; set; }
        public TimeSpan Duration { get => _duration; set { if (_duration != value) _duration = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Duration))); } }
        public bool IsFinalized { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;
    }

    public class TimelineBlock
    {
        public DateTimeOffset Start { get; set; }
        public TimeSpan Duration { get; set; }
        public Brush Color { get; set; }
        public bool IsIdle { get; set; }
        public string Label { get; set; }
    }
}
