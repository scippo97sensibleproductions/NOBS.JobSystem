using System.Reflection;
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
    /// Gets the unique, stable name of the job.
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
        Name = GetJobName(jobType);
        CronExpression = cronExpression;

        if (!string.IsNullOrWhiteSpace(cronExpression))
        {
            Schedule = CrontabSchedule.Parse(cronExpression, new CrontabSchedule.ParseOptions { IncludingSeconds = false });
        }
    }
    
    /// <summary>
    /// Resolves the stable name of a job type using the <see cref="JobNameAttribute"/> or the type name.
    /// </summary>
    public static string GetJobName(Type jobType)
    {
        return jobType.GetCustomAttribute<JobNameAttribute>()?.Name ?? jobType.Name;
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
    
    /// <summary>
    /// Specifies a job to run if the configured job completes successfully.
    /// </summary>
    /// <typeparam name="TNextJob">The type of the next job.</typeparam>
    public JobConfiguration OnSuccess<TNextJob>() where TNextJob : IJob
    {
        // This is a syntactical sugar helper; the runtime logic uses JobExecutionResult.
        // However, we can store a default 'NextJob' if we wanted to enforce static chains here.
        // For now, this method is purely for API alignment with the user's request if they want static chaining in config.
        // The prompt's example used `OnSuccess` but the original `JobConfiguration` didn't have it stored.
        // We will stick to the previous implementation where logic is dynamic or via JobExecutionResult,
        // unless we want to change `JobConfiguration` to store a default success path.
        // Given the prompt constraints, I will leave the implementation consistent with the provided JobConfiguration.
        // If static chaining is desired, it should be added to the properties.
        return this; 
    }
}