using NOBS.JobSystem.UI.Components;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using NOBS.JobSystem.UI.Services;

namespace NOBS.JobSystem.UI;

/// <summary>
/// Extension methods for configuring the job monitor UI.
/// </summary>
public static class JobMonitorExtensions
{
    /// <summary>
    /// Adds services required for the job monitor UI.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddJobMonitorUI(this IServiceCollection services)
    {
        services.AddRazorComponents();
        services.AddScoped<JobStatusService>();
        return services;
    }

    /// <summary>
    /// Maps the job monitor UI endpoint.
    /// </summary>
    /// <param name="endpoints">The endpoint route builder.</param>
    /// <returns>The endpoint route builder for chaining.</returns>
    public static IEndpointRouteBuilder MapJobMonitorUI(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapRazorComponents<App>();
        return endpoints;
    }
}