using NOBS.JobSystem.UI.Components;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using NOBS.JobSystem.UI.Services;

namespace NOBS.JobSystem.UI;

public static class JobMonitorExtensions
{
    public static IServiceCollection AddJobMonitorUI(this IServiceCollection services)
    {
        services.AddRazorComponents();
        services.AddScoped<JobStatusService>();
        return services;
    }

    public static IEndpointRouteBuilder MapJobMonitorUI(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapRazorComponents<App>();
        return endpoints;
    }
}