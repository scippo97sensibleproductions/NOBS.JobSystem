using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NOBS.JobSystem.Abstractions;
using NOBS.JobSystem.Execution;

namespace NOBS.JobSystem.Stores.MongoDb;

/// <summary>
/// Provides extension methods for configuring MongoDB as the persistence provider for the job system.
/// </summary>
public static class MongoDbJobSystemBuilderExtensions
{
    /// <summary>
    /// Configures the job system to use MongoDB for storing job execution history.
    /// </summary>
    /// <param name="builder">The job system builder.</param>
    /// <param name="configure">An action to configure the MongoDB options.</param>
    /// <returns>The job system builder for chaining.</returns>
    public static IJobSystemBuilder UseMongoDb(
        this IJobSystemBuilder builder,
        Action<MongoDbOptions> configure)
    {
        builder.Services.Configure(configure);
        builder.Services.TryAddSingleton<IJobHistoryStore, MongoDbJobHistoryStore>();

        builder.Services.AddOptions<JobSystemOptions>()
            .Configure(options =>
            {
                var mongoOptions = new MongoDbOptions();
                configure(mongoOptions);
                options.PollingFrequency = mongoOptions.PollingFrequency;
            });

        return builder;
    }
}