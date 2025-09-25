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
    public IReadOnlyList<JobConfiguration> JobConfigurations => _jobConfigurations;
    private readonly List<JobConfiguration> _jobConfigurations = [];

    /// <summary>
    /// Adds a job to the registry with a specified CRON schedule.
    /// </summary>
    /// <typeparam name="TJob">The type of the job to add, which must implement <see cref="IJob"/>.</typeparam>
    /// <param name="cronExpression">The CRON expression that defines the job's schedule.</param>
    /// <returns>A <see cref="JobConfiguration"/> object for further setup, such as specifying an error handler.</returns>
    public JobConfiguration AddJob<TJob>(string cronExpression) where TJob : IJob
    {
        var config = new JobConfiguration(typeof(TJob), cronExpression);
        _jobConfigurations.Add(config);
        return config;
    }

    /// <summary>
    /// Adds a job to the registry without a schedule. This is for jobs that are only triggered as continuations or error handlers.
    /// </summary>
    /// <typeparam name="TJob">The type of the job to add, which must implement <see cref="IJob"/>.</typeparam>
    /// <returns>A <see cref="JobConfiguration"/> object for further setup.</returns>
    public JobConfiguration AddJob<TJob>() where TJob : IJob
    {
        var config = new JobConfiguration(typeof(TJob), null);
        _jobConfigurations.Add(config);
        return config;
    }

    internal JobConfiguration? FindByType(Type jobType) =>
        _jobConfigurations.FirstOrDefault(c => c.JobType == jobType);
    
    internal JobConfiguration? FindByName(string jobName) =>
        _jobConfigurations.FirstOrDefault(c => c.Name.Equals(jobName, StringComparison.OrdinalIgnoreCase));
}