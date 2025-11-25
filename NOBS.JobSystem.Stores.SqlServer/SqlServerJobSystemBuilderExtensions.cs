using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NOBS.JobSystem.Abstractions;
using NOBS.JobSystem.Execution;
using NOBS.JobSystem.Stores.SqlServer.Persistence;

namespace NOBS.JobSystem.Stores.SqlServer;

public static class SqlServerJobSystemBuilderExtensions
{
    extension(IJobSystemBuilder builder)
    {
        /// <summary>
        /// Configures the job system to use SQL Server using a manual configuration delegate.
        /// </summary>
        public IJobSystemBuilder UseSqlServer(Action<SqlServerOptions> configure)
        {
            builder.Services.Configure(configure);
            return RegisterSqlServerServices(builder);
        }

        /// <summary>
        /// Configures the job system to use SQL Server by binding options from the specified configuration section.
        /// </summary>
        public IJobSystemBuilder UseSqlServer(IConfiguration configuration)
        {
            builder.Services.Configure<SqlServerOptions>(configuration);
            return RegisterSqlServerServices(builder);
        }
    }

    private static IJobSystemBuilder RegisterSqlServerServices(IJobSystemBuilder builder)
    {
        builder.Services.AddDbContextFactory<JobDbContext>((sp, dbOptions) =>
        {
            var options = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<SqlServerOptions>>().Value;
            dbOptions.UseSqlServer(options.ConnectionString);
        });

        builder.Services.TryAddSingleton<IJobHistoryStore, SqlServerJobHistoryStore>();

        // Map the provider specific PollingFrequency to the global JobSystemOptions
        builder.Services.AddOptions<JobSystemOptions>()
            .Configure<Microsoft.Extensions.Options.IOptions<SqlServerOptions>>((options, sqlOptions) =>
            {
                options.PollingFrequency = sqlOptions.Value.PollingFrequency;
            });
        
        return builder;
    }
}