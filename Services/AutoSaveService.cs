using System;
using System.Collections.Generic;
using System.Windows.Threading;
using TimeTracker.Models;

namespace TimeTracker.Services
{
    public class AutoSaveService : IDisposable
    {
        private readonly DispatcherTimer _timer;
        private readonly ITaskPersistenceService _persistence;
        private readonly IAppSettings _settings;
        private readonly Func<IEnumerable<TaskModel>> _getTasks;

        public AutoSaveService(
            ITaskPersistenceService persistence,
            IAppSettings settings,
            Func<IEnumerable<TaskModel>> getTasks)
        {
            _persistence = persistence;
            _settings = settings;
            _getTasks = getTasks;

            _timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };

            _timer.Tick += (_, __) => Save();
            _timer.Start();
        }

        private void Save()
        {
            var tasks = _getTasks();
            _persistence.Save(_settings.TasksFilePath, tasks);
        }

        public void Dispose()
        {
            _timer.Stop();
        }
    }

}
