using NCrontab;
using NOBS.JobSystem.Abstractions;

namespace NOBS.JobSystem.Execution;

/// <summary>
/// Holds the configuration for a single registered job, including its type, schedule, and error handling.
/// </summary>
public sealed class JobConfiguration
{
    /// <summary>
    /// Gets the concrete type of the job.
    /// </summary>
    public Type JobType { get; }

    /// <summary>
    /// Gets the full name of the job type.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the CRON expression for the job's schedule, if any.
    /// </summary>
    public string? CronExpression { get; }

    /// <summary>
    /// Gets the parsed CRON schedule, if any.
    /// </summary>
    public CrontabSchedule? Schedule { get; }

    /// <summary>
    /// Gets the job type to execute if this job throws an unhandled exception.
    /// </summary>
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

    /// <summary>
    /// Specifies a job to run if the configured job throws an unhandled exception.
    /// </summary>
    /// <typeparam name="TErrorJob">The type of the error-handling job.</typeparam>
    /// <returns>The same <see cref="JobConfiguration"/> instance for chaining.</returns>
    public JobConfiguration OnError<TErrorJob>() where TErrorJob : IJob
    {
        ErrorJobType = typeof(TErrorJob);
        return this;
    }
}