using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using NOBS.JobSystem.Execution;
using NOBS.JobSystem.UI;

namespace NOBS.JobSystem.Hosting;

public static class JobSystemHostingExtensions
{
    public static IServiceCollection AddHostedJobSystem(
        this IServiceCollection services,
        Action<JobSystemOptions> configureOptions,
        Action<JobRegistry> configureJobs)
    {
        services.AddJobSystem(configureOptions, configureJobs);
        services.AddJobMonitorUI();
        return services;
    }

    public static IEndpointRouteBuilder MapHostedJobSystemUI(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapJobMonitorUI();
        return endpoints;
    }
}