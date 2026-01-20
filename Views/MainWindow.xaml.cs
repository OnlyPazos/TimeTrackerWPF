using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using TimeTracker.ViewModels;

namespace TimeTracker.Views
{
    public partial class MainWindow : Window
    {
        private MainViewModel vm => DataContext as MainViewModel;

        private readonly List<Border> _timelineBlocks = new List<Border>();
        private TimelineBlock _lastBlock;
        private bool _timelineDirty = true;

        private readonly Dictionary<TaskViewModel, ColumnDefinition> _taskColumns = new Dictionary<TaskViewModel, ColumnDefinition>();
        private bool _taskBarDirty = true;
        private readonly HashSet<TaskViewModel> _visibleTasks = new HashSet<TaskViewModel>();

        public MainWindow()
        {
            InitializeComponent();
            Loaded += MainWindow_Loaded;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs _e)
        {
            foreach (var task in vm.Tasks)
            {
                task.PropertyChanged += Task_PropertyChanged;

                var last = task.Intervals.LastOrDefault();
                if (last != null && !last.IsFinalized)
                {
                    var elapsed = DateTime.Now - last.Start;

                    bool wasResumed = task.ResumeFromInterval(last);
                    vm.SelectedTask = wasResumed ? task : null;
                    break;
                }
            }

            vm.PropertyChanged += (s, ev) =>
            {
                if (ev.PropertyName == nameof(vm.TotalTime))
                {
                    BuildTimeline();
                }
            };

            vm.TimelineStructureChanged += () =>
            {
                _timelineDirty = true;
            };

            // Actualiza cuando cambia la colección
            vm.Tasks.CollectionChanged += (s, e) =>
            {
                if (e.NewItems != null)
                {
                    foreach (TaskViewModel t in e.NewItems)
                        t.PropertyChanged += Task_PropertyChanged;
                }

                if (e.OldItems != null)
                {
                    foreach (TaskViewModel t in e.OldItems)
                        t.PropertyChanged -= Task_PropertyChanged;
                }

                _taskBarDirty = true;
            };

            TimelineCanvas.SizeChanged += (_, __) =>
            {
                _timelineDirty = true;
            };

            CompositionTarget.Rendering += (_, __) =>
            {
                UpdateTimeline();
                UpdateTaskBar();
            };

            BuildTimeline();
            BuildTaskBar();
        }

        private void Task_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (!(sender is TaskViewModel task)) return;

            if (e.PropertyName == nameof(TaskViewModel.IsRunning))
            {
                _timelineDirty = true;
            }

            if (e.PropertyName == nameof(TaskViewModel.Total))
            {
                bool isVisible = task.Total.TotalMilliseconds > 0;
                bool wasVisible = _visibleTasks.Contains(task);

                if (isVisible && !wasVisible)
                {
                    _taskBarDirty = true;
                }
            }
        }

        private void UpdateTimeline()
        {
            if (_timelineDirty)
            {
                BuildTimeline();
                _timelineDirty = false;
                return;
            }

            if (_lastBlock == null || !_lastBlock.IsIdle)
            {
                // último bloque activo
                var lastBorder = _timelineBlocks.LastOrDefault();
                if (lastBorder == null) return;

                double canvasWidth = TimelineCanvas.ActualWidth;
                double totalMs = vm.TimelineTotal.TotalMilliseconds;

                double startMs = (_lastBlock.Start - vm.TimelineStart).TotalMilliseconds;
                double widthMs = _lastBlock.Duration.TotalMilliseconds;

                double left = (startMs / totalMs) * canvasWidth;
                double width = (widthMs / totalMs) * canvasWidth;

                lastBorder.Width = width;
                Canvas.SetLeft(lastBorder, left);
            }
        }

        private void UpdateTaskBar()
        {
            if (_taskBarDirty)
            {
                BuildTaskBar();
                _taskBarDirty = false;
            }

            foreach (var item in _taskColumns)
            {
                item.Value.Width = new GridLength(item.Key.DailyProgress, GridUnitType.Star);
            }
        }

        private void UpdateTimelineBlock(Border border, TimelineBlock block, double canvasWidth, double totalMs)
        {
            double startMs = (block.Start - vm.TimelineStart).TotalMilliseconds;
            double widthMs = block.Duration.TotalMilliseconds;

            double left = (startMs / totalMs) * canvasWidth;
            double width = (widthMs / totalMs) * canvasWidth;

            if (width < 0)
                width = 0;

            border.Width = width;
            border.Height = TimelineCanvas.ActualHeight;

            Canvas.SetLeft(border, left);
            Canvas.SetTop(border, 0);
        }


        private void BuildTimeline()
        {
            if (vm == null || TimelineCanvas.ActualWidth <= 0)
                return;

            TimelineCanvas.Children.Clear();
            _timelineBlocks.Clear();

            double canvasWidth = TimelineCanvas.ActualWidth;
            double totalMs = vm.TimelineTotal.TotalMilliseconds;
            double blockHeight = TimelineCanvas.ActualHeight;

            var timelineBlocks = vm.BuildTimelineBlocks();
            foreach (var block in timelineBlocks)
            {
                var border = new Border
                {
                    Height = blockHeight,
                    Background = block.Color,
                    CornerRadius = new CornerRadius(5),
                    Opacity = block.IsIdle ? 0.3 : 1,
                    ToolTip = block.Label
                };

                TimelineCanvas.Children.Add(border);
                _timelineBlocks.Add(border);

                UpdateTimelineBlock(border, block, canvasWidth, totalMs);
                _lastBlock = block;
            }
        }

        private void BuildTaskBar()
        {
            if (vm == null) return;

            DistributionBar.Children.Clear();
            DistributionBar.ColumnDefinitions.Clear();
            _taskColumns.Clear();
            _visibleTasks.Clear();

            int colIndex = 0;
            bool first = true;
            foreach (var task in vm.Tasks)
            {
                if (task.DailyTotal.TotalMilliseconds <= 0)
                    continue;

                if (!first)
                {
                    var colGap = new ColumnDefinition
                    {
                        Width = new GridLength(4, GridUnitType.Pixel)
                    };
                    DistributionBar.ColumnDefinitions.Add(colGap);
                    colIndex++;
                }

                first = false;
                _visibleTasks.Add(task);

                var col = new ColumnDefinition
                {
                    Width = new GridLength(task.DailyProgress, GridUnitType.Star)
                };

                DistributionBar.ColumnDefinitions.Add(col);
                _taskColumns[task] = col;

                var border = new Border
                {
                    Background = task.Color,
                    Margin = new Thickness(1),
                    CornerRadius = new CornerRadius(5)
                };

                DistributionBar.Children.Add(border);
                Grid.SetColumn(border, colIndex++);
            }

        }
    }
}
