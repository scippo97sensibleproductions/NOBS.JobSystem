using Microsoft.Extensions.Configuration;
using NOBS.JobSystem.Abstractions;

namespace NOBS.JobSystem.Execution;

/// <summary>
/// A registry for discovering and configuring jobs, with optional support for external configuration.
/// </summary>
public sealed class JobRegistry(IConfiguration? configuration = null)
{
    /// <summary>
    /// Gets the list of job configurations.
    /// </summary>
    public IReadOnlyList<JobConfiguration> JobConfigurations => _jobConfigurations;
    private readonly List<JobConfiguration> _jobConfigurations = [];

    /// <summary>
    /// Adds a job to the registry. The schedule is determined in the following order:
    /// 1. Configuration (if a value exists in "Jobs:{JobName}").
    /// 2. The provided <paramref name="cronExpression"/>.
    /// </summary>
    /// <typeparam name="TJob">The type of the job to add, which must implement <see cref="IJob"/>.</typeparam>
    /// <param name="cronExpression">The default CRON expression to use if not overridden by configuration.</param>
    /// <returns>A <see cref="JobConfiguration"/> object for further setup.</returns>
    public JobConfiguration AddJob<TJob>(string cronExpression) where TJob : IJob
    {
        var jobName = JobConfiguration.GetJobName(typeof(TJob));
        var resolvedCron = ResolveCronExpression(jobName) ?? cronExpression;
        
        var config = new JobConfiguration(typeof(TJob), resolvedCron);
        _jobConfigurations.Add(config);
        return config;
    }

    /// <summary>
    /// Adds a job to the registry without a hardcoded schedule.
    /// A schedule will only be applied if one is found in the configuration.
    /// </summary>
    /// <typeparam name="TJob">The type of the job to add, which must implement <see cref="IJob"/>.</typeparam>
    /// <returns>A <see cref="JobConfiguration"/> object for further setup.</returns>
    public JobConfiguration AddJob<TJob>() where TJob : IJob
    {
        var jobName = JobConfiguration.GetJobName(typeof(TJob));
        var resolvedCron = ResolveCronExpression(jobName);
        
        var config = new JobConfiguration(typeof(TJob), resolvedCron);
        _jobConfigurations.Add(config);
        return config;
    }

    private string? ResolveCronExpression(string jobName)
    {
        if (configuration is null)
        {
            return null;
        }

        var configSection = configuration.GetSection($"Jobs:{jobName}");
        var value = configSection.Value;

        return string.IsNullOrWhiteSpace(value) ? null : value;
    }

    internal JobConfiguration? FindByType(Type jobType) =>
        _jobConfigurations.FirstOrDefault(c => c.JobType == jobType);
    
    internal JobConfiguration? FindByName(string jobName) =>
        _jobConfigurations.FirstOrDefault(c => c.Name.Equals(jobName, StringComparison.OrdinalIgnoreCase));
}