namespace NOBS.JobSystem.Abstractions;

public sealed record JobExecutionResult
{
    public bool Succeeded { get; private init; }
    public Type? NextJobTypeOnSuccess { get; private init; }
    public Type? NextJobTypeOnError { get; private init; }

    public static JobExecutionResult Success() => new() { Succeeded = true };

    public static JobExecutionResult Success(Type nextJob) => new()
    {
        Succeeded = true,
        NextJobTypeOnSuccess = nextJob
    };

    public static JobExecutionResult Failure() => new() { Succeeded = false };

    public static JobExecutionResult Failure(Type nextJob) => new()
    {
        Succeeded = false,
        NextJobTypeOnError = nextJob
    };
}