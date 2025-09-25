using System.Collections.Concurrent;

namespace NOBS.JobSystem.Execution;

internal sealed class InMemoryJobExecutionTracker : IJobExecutionTracker
{
    private readonly ConcurrentDictionary<string, byte> _runningJobs = new();

    public bool TryMarkAsRunning(string jobName) => _runningJobs.TryAdd(jobName, 0);

    public void MarkAsCompleted(string jobName) => _runningJobs.TryRemove(jobName, out _);
}