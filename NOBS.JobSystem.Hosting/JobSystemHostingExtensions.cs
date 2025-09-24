using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using NOBS.JobSystem.Execution;
using NOBS.JobSystem.UI;

namespace NOBS.JobSystem.Hosting;

/// <summary>
/// Extension methods for hosting the job system and its UI in an ASP.NET Core application.
/// </summary>
public static class JobSystemHostingExtensions
{
    /// <summary>
    /// Adds the job system and the monitoring UI to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configureOptions">An action to configure the job system options.</param>
    /// <param name="configureJobs">An action to register jobs.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddHostedJobSystem(
        this IServiceCollection services,
        Action<JobSystemOptions> configureOptions,
        Action<JobRegistry> configureJobs)
    {
        services.AddJobSystem(configureOptions, configureJobs);
        services.AddJobMonitorUI();
        return services;
    }

    /// <summary>
    /// Maps the job system monitoring UI to the default endpoint "/jobs".
    /// </summary>
    /// <param name="endpoints">The endpoint route builder.</param>
    /// <returns>The endpoint route builder for chaining.</returns>
    public static IEndpointRouteBuilder MapHostedJobSystemUI(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapJobMonitorUI();
        return endpoints;
    }
}