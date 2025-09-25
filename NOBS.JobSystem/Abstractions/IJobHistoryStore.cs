namespace NOBS.JobSystem.Abstractions;

/// <summary>
/// Defines the contract for a persistence provider that stores and retrieves job execution history.
/// </summary>
public interface IJobHistoryStore
{
    /// <summary>
    /// Gets the last successful run timestamps for a collection of jobs.
    /// </summary>
    /// <param name="jobNames">The names of the jobs to query.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A dictionary where the key is the job name and the value is the last successful run time in UTC.</returns>
    Task<IReadOnlyDictionary<string, DateTime>> GetLastRunTimesAsync(IEnumerable<string> jobNames, CancellationToken cancellationToken);

    /// <summary>
    /// Sets the last successful run timestamp for a specific job.
    /// </summary>
    /// <param name="jobName">The name of the job to update.</param>
    /// <param name="lastSuccessfulRun">The timestamp of the successful run in UTC.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    Task SetLastSuccessfulRunAsync(string jobName, DateTime lastSuccessfulRun, CancellationToken cancellationToken);
    
    /// <summary>
    /// Initializes the storage provider, performing any necessary setup like creating tables or files.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    Task InitializeAsync(CancellationToken cancellationToken);
}