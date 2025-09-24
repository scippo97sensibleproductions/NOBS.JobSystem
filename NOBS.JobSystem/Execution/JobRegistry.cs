using NOBS.JobSystem.Abstractions;

namespace NOBS.JobSystem.Execution;

/// <summary>
/// A registry for discovering and configuring jobs.
/// </summary>
public sealed class JobRegistry
{
    /// <summary>
    /// Gets the list of job configurations.
    /// </summary>
    public List<JobConfiguration> JobConfigurations { get; } = [];

    /// <summary>
    /// Adds a job to the registry with a specified CRON schedule.
    /// </summary>
    /// <typeparam name="TJob">The type of the job to add.</typeparam>
    /// <param name="cronExpression">The CRON expression that defines the job's schedule.</param>
    /// <returns>A <see cref="JobConfiguration"/> object for further setup.</returns>
    public JobConfiguration AddJob<TJob>(string cronExpression) where TJob : IJob
    {
        var config = new JobConfiguration(typeof(TJob), cronExpression);
        JobConfigurations.Add(config);
        return config;
    }

    internal JobConfiguration? FindByType(Type jobType) =>
        JobConfigurations.FirstOrDefault(c => c.JobType == jobType);
}