namespace NOBS.JobSystem.Abstractions;

/// <summary>
/// Represents a job that can be executed by the job system.
/// </summary>
public interface IJob
{
    /// <summary>
    /// Executes the job's logic.
    /// </summary>
    /// <param name="cancellationToken">A token to signal that the operation should be cancelled.</param>
    /// <returns>A <see cref="JobExecutionResult"/> indicating the outcome of the execution.</returns>
    Task<JobExecutionResult> ExecuteAsync(CancellationToken cancellationToken);
}