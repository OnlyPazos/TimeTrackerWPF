using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using TimeTracker.Models;
using TimeTracker.ViewModels;

namespace TimeTracker.Services
{
    public class JsonTaskPersistenceService: ITaskPersistenceService
    {
        public void Save(string path, IEnumerable<TaskModel> tasks)
        {
            var json = JsonConvert.SerializeObject(tasks, Formatting.Indented);
            File.WriteAllText(path, json);
        }

        public IEnumerable<TaskModel> Load(string path)
        {
            if (!File.Exists(path))
                return Enumerable.Empty<TaskModel>();

            var json = File.ReadAllText(path);
            return JsonConvert.DeserializeObject<List<TaskModel>>(json)
                   ?? Enumerable.Empty<TaskModel>();
        }

    }
}
