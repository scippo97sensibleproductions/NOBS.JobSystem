namespace NOBS.JobSystem.Abstractions;

public interface IJob
{
    Task<JobExecutionResult> ExecuteAsync(CancellationToken cancellationToken);
}