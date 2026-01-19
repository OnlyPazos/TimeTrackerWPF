using System;
using System.Collections.Generic;
using TimeTracker.ViewModels;

namespace TimeTracker.Models
{
    public class TaskModel
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Name { get; set; } = "";
        public string ColorHex { get; set; } = "#888888";
        public List<TimeInterval> Intervals { get; set; } = new List<TimeInterval>();
    }
}
