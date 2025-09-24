using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NOBS.JobSystem.Persistence;

namespace NOBS.JobSystem.Execution;

public static class JobSystemExtensions
{
    public static IServiceCollection AddJobSystem(
        this IServiceCollection services,
        Action<JobSystemOptions> configureOptions,
        Action<JobRegistry> configureJobs)
    {
        var options = new JobSystemOptions();
        configureOptions(options);

        if (string.IsNullOrWhiteSpace(options.ConnectionString))
        {
            throw new ArgumentException("A connection string must be provided for the job system.", nameof(options.ConnectionString));
        }

        services.AddSingleton(options);

        services.AddDbContextFactory<JobDbContext>(dbOptions =>
            dbOptions.UseSqlServer(options.ConnectionString));

        services.AddHostedService<DatabaseInitializer>();

        var registry = new JobRegistry();
        configureJobs(registry);

        foreach (var config in registry.JobConfigurations)
        {
            services.AddScoped(config.JobType);
            if (config.ErrorJobType != null && config.JobType != config.ErrorJobType)
            {
                services.AddScoped(config.ErrorJobType);
            }
        }

        services.AddSingleton(registry);
        services.AddSingleton<JobOrchestrator>();
        services.AddHostedService<ScheduledJobService>();
        
        return services;
    }
}