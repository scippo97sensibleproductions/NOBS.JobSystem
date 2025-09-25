namespace NOBS.JobSystem.Execution;

/// <summary>
/// Defines core configuration options for the job system's background service.
/// </summary>
public class JobSystemOptions
{
    /// <summary>
    /// The frequency at which the system checks for due jobs. Defaults to one minute.
    /// This value can be overridden by provider-specific options.
    /// </summary>
    public TimeSpan PollingFrequency { get; set; } = TimeSpan.FromMinutes(1);
}