using Microsoft.Extensions.DependencyInjection;

namespace NOBS.JobSystem.Execution;

/// <summary>
/// Provides a fluent API for configuring the job system after it has been added to the service collection.
/// </summary>
public interface IJobSystemBuilder
{
    /// <summary>
    /// Gets the <see cref="IServiceCollection"/> where the job system is being configured.
    /// </summary>
    IServiceCollection Services { get; }
}