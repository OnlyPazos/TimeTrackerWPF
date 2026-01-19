using System.Collections.Generic;
using TimeTracker.Models;

namespace TimeTracker.Services
{
    public interface ITaskPersistenceService
    {
        void Save(string path, IEnumerable<TaskModel> tasks);
        IEnumerable<TaskModel> Load(string path);
    }
}
