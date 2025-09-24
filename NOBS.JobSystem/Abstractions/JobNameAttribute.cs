namespace NOBS.JobSystem.Abstractions;

/// <summary>
/// Assigns a stable, unique name to an IJob implementation.
/// This name is used as the persistent identifier in the database, decoupling the job's identity
/// from its class name or namespace, which may change during refactoring.
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public sealed class JobNameAttribute(string name) : Attribute
{
    /// <summary>
    /// Gets the unique name of the job.
    /// </summary>
    public string Name { get; } = name;
}