using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NOBS.JobSystem.Persistence;

namespace NOBS.JobSystem.Execution;

/// <summary>
/// Provides extension methods for setting up the job system in the DI container.
/// </summary>
public static class JobSystemExtensions
{
    /// <summary>
    /// Adds the core job system services to the specified <see cref="IServiceCollection"/>.
    /// </summary>
    /// <param name="services">The service collection to add the services to.</param>
    /// <param name="configureOptions">An action to configure the <see cref="JobSystemOptions"/>.</param>
    /// <param name="configureJobs">An action to configure the jobs in the <see cref="JobRegistry"/>.</param>
    /// <returns>The <see cref="IServiceCollection"/> so that additional calls can be chained.</returns>
    public static IServiceCollection AddJobSystem(
        this IServiceCollection services,
        Action<JobSystemOptions> configureOptions,
        Action<JobRegistry> configureJobs)
    {
        services.AddOptions<JobSystemOptions>()
            .Configure(configureOptions)
            .ValidateDataAnnotations()
            .ValidateOnStart();
        
        services.AddSingleton(sp => sp.GetRequiredService<IOptions<JobSystemOptions>>().Value);

        services.AddDbContextFactory<JobDbContext>((sp, dbOptions) =>
        {
            var options = sp.GetRequiredService<IOptions<JobSystemOptions>>().Value;
            dbOptions.UseSqlServer(options.ConnectionString);
        });

        services.AddHostedService<DatabaseInitializer>();

        var registry = new JobRegistry();
        configureJobs(registry);

        var allJobTypes = registry.JobConfigurations
            .SelectMany(c => new[] { c.JobType, c.ErrorJobType })
            .Where(t => t is not null)
            .Distinct();

        foreach (var jobType in allJobTypes)
        {
            services.AddScoped(jobType!);
        }

        services.AddSingleton(registry);
        services.AddSingleton<JobOrchestrator>();
        services.AddHostedService<ScheduledJobService>();

        return services;
    }
}