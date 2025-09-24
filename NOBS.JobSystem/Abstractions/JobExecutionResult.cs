namespace NOBS.JobSystem.Abstractions;

/// <summary>
/// Represents the result of a job's execution.
/// </summary>
public sealed record JobExecutionResult
{
    private static readonly JobExecutionResult SuccessInstance = new() { Succeeded = true };
    private static readonly JobExecutionResult FailureInstance = new() { Succeeded = false };

    /// <summary>
    /// Gets a value indicating whether the job succeeded.
    /// </summary>
    public bool Succeeded { get; private init; }

    /// <summary>
    /// Gets the type of the next job to run if this job completed successfully.
    /// </summary>
    public Type? NextJobTypeOnSuccess { get; private init; }

    /// <summary>
    /// Gets the type of the next job to run if this job failed.
    /// </summary>
    public Type? NextJobTypeOnError { get; private init; }

    /// <summary>
    /// Creates a result indicating successful execution with no continuation job.
    /// </summary>
    /// <returns>A singleton result object indicating success.</returns>
    public static JobExecutionResult Success() => SuccessInstance;

    /// <summary>
    /// Creates a result indicating successful execution and specifies a continuation job.
    /// </summary>
    /// <param name="nextJob">The type of the job to execute next.</param>
    /// <returns>A new result object indicating success with a continuation.</returns>
    public static JobExecutionResult Success(Type nextJob) => new()
    {
        Succeeded = true,
        NextJobTypeOnSuccess = nextJob
    };

    /// <summary>
    /// Creates a result indicating failed execution with no continuation job.
    /// </summary>
    /// <returns>A singleton result object indicating failure.</returns>
    public static JobExecutionResult Failure() => FailureInstance;

    /// <summary>
    /// Creates a result indicating failed execution and specifies a continuation job.
    /// </summary>
    /// <param name="nextJob">The type of the job to execute next.</param>
    /// <returns>A new result object indicating failure with a continuation.</returns>
    public static JobExecutionResult Failure(Type nextJob) => new()
    {
        Succeeded = false,
        NextJobTypeOnError = nextJob
    };
}