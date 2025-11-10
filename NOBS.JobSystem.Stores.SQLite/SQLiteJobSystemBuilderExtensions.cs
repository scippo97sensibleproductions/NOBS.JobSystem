using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NOBS.JobSystem.Abstractions;
using NOBS.JobSystem.Execution;
using NOBS.JobSystem.Stores.SQLite.Persistence;

namespace NOBS.JobSystem.Stores.SQLite;

/// <summary>
/// Provides extension methods for configuring SQLite as the persistence provider for the job system.
/// </summary>
public static class SQLiteJobSystemBuilderExtensions
{
    /// <summary>
    /// Configures the job system to use SQLite for storing job execution history.
    /// </summary>
    /// <param name="builder">The job system builder.</param>
    /// <param name="configure">An action to configure the SQLite options.</param>
    /// <returns>The job system builder for chaining.</returns>
    public static IJobSystemBuilder UseSQLite(
        this IJobSystemBuilder builder,
        Action<SQLiteOptions> configure)
    {
        builder.Services.Configure(configure);

        builder.Services.AddDbContextFactory<JobDbContext>((sp, dbOptions) =>
        {
            var options = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<SQLiteOptions>>().Value;
            dbOptions.UseSqlite(options.ConnectionString);
        });

        builder.Services.TryAddSingleton<IJobHistoryStore, SQLiteJobHistoryStore>();

        builder.Services.AddOptions<JobSystemOptions>()
            .Configure(options =>
            {
                var sqlOptions = new SQLiteOptions();
                configure(sqlOptions);
                options.PollingFrequency = sqlOptions.PollingFrequency;
            });

        return builder;
    }
}