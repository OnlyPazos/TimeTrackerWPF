using System;

namespace TimeTracker.Providers
{
    public interface ITotalTimeProvider
    {
        TimeSpan TotalTime { get; }
    }
}
