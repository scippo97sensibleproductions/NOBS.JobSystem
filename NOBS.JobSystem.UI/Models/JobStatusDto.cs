namespace NOBS.JobSystem.UI.Models;

/// <summary>
/// A data transfer object representing the status of a registered job.
/// </summary>
/// <param name="jobName">The name of the job.</param>
/// <param name="cronExpression">The job's CRON schedule.</param>
public sealed class JobStatusDto(string jobName, string cronExpression)
{
    /// <summary>
    /// Gets the name of the job.
    /// </summary>
    public string JobName { get; } = jobName;

    /// <summary>
    /// Gets the job's CRON schedule.
    /// </summary>
    public string CronExpression { get; } = cronExpression;

    /// <summary>
    /// Gets or sets the last successful run time in UTC.
    /// </summary>
    public DateTimeOffset? LastRunUtc { get; set; }

    /// <summary>
    /// Gets or sets the next scheduled run time in UTC.
    /// </summary>
    public DateTimeOffset? NextRunUtc { get; set; }
}