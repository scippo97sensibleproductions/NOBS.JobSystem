using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using NOBS.JobSystem.Abstractions;
using NOBS.JobSystem.UI;

namespace NOBS.JobSystem.Hosting;

public static class JobSystemHostingExtensions
{
    public static IEndpointRouteBuilder MapHostedJobSystemUI(this IEndpointRouteBuilder endpoints)
    {
        var routeHandler = endpoints.MapPost("/jobs/trigger", async (
            [FromForm] string jobName,
            [FromServices] IJobTrigger jobTrigger,
            HttpContext httpContext) =>
        {
            if (!string.IsNullOrWhiteSpace(jobName))
            {
                await jobTrigger.TriggerJobAsync(jobName);
            }
            httpContext.Response.Headers.Location = "/jobs";
            return Results.StatusCode(303);
        });
        
#if NET7_0_OR_GREATER
        routeHandler.DisableAntiforgery();
#endif
        
        endpoints.MapJobMonitorUI();
        
        return endpoints;
    }
}