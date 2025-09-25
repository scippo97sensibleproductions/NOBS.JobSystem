namespace NOBS.JobSystem.Execution;

internal interface IJobExecutionTracker
{
    bool TryMarkAsRunning(string jobName);
    
    void MarkAsCompleted(string jobName);
}