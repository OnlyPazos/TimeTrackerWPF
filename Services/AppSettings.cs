using System;
using System.IO;

namespace TimeTracker.Services
{
    public class AppSettings : IAppSettings
    {
        public string TasksFilePath =>
            Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                "tasks.json"
            );
    }

}
