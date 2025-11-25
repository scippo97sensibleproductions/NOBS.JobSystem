using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NOBS.JobSystem.Abstractions;

namespace NOBS.JobSystem.Execution;

/// <summary>
/// Provides extension methods for setting up the job system in the DI container.
/// </summary>
public static class JobSystemExtensions
{
    /// <param name="services">The service collection to add the services to.</param>
    extension(IServiceCollection services)
    {
        /// <summary>
        /// Adds the core job system services to the specified <see cref="IServiceCollection"/> using manual configuration.
        /// </summary>
        /// <param name="configureJobs">An action to configure the jobs in the <see cref="JobRegistry"/>.</param>
        /// <returns>An <see cref="IJobSystemBuilder"/> for chaining storage provider configuration.</returns>
        public IJobSystemBuilder AddJobSystem(Action<JobRegistry> configureJobs)
        {
            return services.AddJobSystem(null, configureJobs);
        }

        /// <summary>
        /// Adds the core job system services to the specified <see cref="IServiceCollection"/> using <see cref="IConfiguration"/>.
        /// </summary>
        /// <param name="configuration">The configuration section (e.g. "JobSystem") containing settings and job schedules.</param>
        /// <param name="configureJobs">An action to register job types. Schedules defined in configuration take precedence.</param>
        /// <returns>An <see cref="IJobSystemBuilder"/> for chaining storage provider configuration.</returns>
        public IJobSystemBuilder AddJobSystem(IConfiguration? configuration,
            Action<JobRegistry> configureJobs)
        {
            var registry = new JobRegistry(configuration);
            configureJobs(registry);
            ValidateJobNameUniqueness(registry);

            services.TryAddSingleton(registry);
            services.TryAddSingleton<IJobExecutionTracker, InMemoryJobExecutionTracker>();
            services.TryAddSingleton<JobOrchestrator>();
            services.TryAddSingleton<IJobTrigger, InMemoryJobTrigger>();

            if (configuration is not null)
            {
                services.Configure<JobSystemOptions>(configuration);
            }

            services.AddHostedService(sp =>
            {
                var options = sp.GetService<Microsoft.Extensions.Options.IOptions<JobSystemOptions>>()?.Value ?? new JobSystemOptions();
                return new ScheduledJobService(
                    sp.GetRequiredService<JobOrchestrator>(),
                    sp.GetRequiredService<IJobTrigger>(),
                    options.PollingFrequency,
                    sp.GetRequiredService<ILogger<ScheduledJobService>>());
            });
        
            services.AddHostedService<StorageInitializer>();

            return new JobSystemBuilder(services);
        }
    }

    private static void ValidateJobNameUniqueness(JobRegistry registry)
    {
        var duplicateNames = registry.JobConfigurations
            .GroupBy(c => c.Name)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToList();

        if (duplicateNames.Any())
        {
            throw new InvalidOperationException(
                "Duplicate job names were detected. Ensure each job has a unique name via the [JobName] attribute or a unique class name. " +
                $"Duplicates found: {string.Join(", ", duplicateNames)}");
        }
    }
    
    private class JobSystemBuilder(IServiceCollection services) : IJobSystemBuilder
    {
        public IServiceCollection Services { get; } = services;
    }
    
    private sealed class StorageInitializer(
        IServiceProvider serviceProvider,
        ILogger<StorageInitializer> logger) : IHostedService
    {
        public async Task StartAsync(CancellationToken cancellationToken)
        {
            logger.LogInformation("Initializing job system storage provider.");

            await using var scope = serviceProvider.CreateAsyncScope();
            var store = scope.ServiceProvider.GetService<IJobHistoryStore>();

            if (store is null)
            {
                logger.LogWarning("No IJobHistoryStore is registered. The job system will run without persistence.");
                return;
            }

            try
            {
                await store.InitializeAsync(cancellationToken);
                logger.LogInformation("Job system storage provider initialized successfully.");
            }
            catch (Exception ex)
            {
                logger.LogCritical(ex, "Failed to initialize the job system storage provider.");
                throw;
            }
        }

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}