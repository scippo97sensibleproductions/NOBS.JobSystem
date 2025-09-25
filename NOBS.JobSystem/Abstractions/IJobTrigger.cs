namespace NOBS.JobSystem.Abstractions;

/// <summary>
/// Defines the contract for a service that can trigger jobs for immediate execution.
/// </summary>
public interface IJobTrigger
{
    /// <summary>
    /// Adds a job to the queue for immediate execution.
    /// </summary>
    /// <param name="jobName">The name of the job to trigger.</param>
    ValueTask TriggerJobAsync(string jobName);

    /// <summary>
    /// Gets an asynchronous stream of job names that have been triggered for execution.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the enumeration.</param>
    /// <returns>An async enumerable of triggered job names.</returns>
    IAsyncEnumerable<string> GetTriggeredJobsAsync(CancellationToken cancellationToken);
}