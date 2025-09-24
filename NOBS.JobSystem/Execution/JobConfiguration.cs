using NCrontab;
using NOBS.JobSystem.Abstractions;

namespace NOBS.JobSystem.Execution;

public sealed class JobConfiguration
{
    public Type JobType { get; }
    public string Name { get; }
    public string? CronExpression { get; }
    public CrontabSchedule? Schedule { get; }
    public Type? ErrorJobType { get; private set; }

    internal JobConfiguration(Type jobType, string? cronExpression)
    {
        if (!typeof(IJob).IsAssignableFrom(jobType))
        {
            throw new ArgumentException($"Type {jobType.Name} must implement {nameof(IJob)}.", nameof(jobType));
        }

        JobType = jobType;
        Name = jobType.FullName ?? jobType.Name;
        CronExpression = cronExpression;

        if (!string.IsNullOrWhiteSpace(cronExpression))
        {
            Schedule = CrontabSchedule.Parse(cronExpression, new CrontabSchedule.ParseOptions { IncludingSeconds = false });
        }
    }

    public JobConfiguration OnError<TErrorJob>() where TErrorJob : IJob
    {
        ErrorJobType = typeof(TErrorJob);
        return this;
    }
}