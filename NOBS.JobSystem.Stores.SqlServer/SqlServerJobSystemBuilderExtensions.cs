using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NOBS.JobSystem.Abstractions;
using NOBS.JobSystem.Execution;
using NOBS.JobSystem.Stores.SqlServer.Persistence;

namespace NOBS.JobSystem.Stores.SqlServer;

public static class SqlServerJobSystemBuilderExtensions
{
    public static IJobSystemBuilder UseSqlServer(
        this IJobSystemBuilder builder,
        Action<SqlServerOptions> configure)
    {
        builder.Services.Configure(configure);

        builder.Services.AddDbContextFactory<JobDbContext>((sp, dbOptions) =>
        {
            var options = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<SqlServerOptions>>().Value;
            dbOptions.UseSqlServer(options.ConnectionString);
        });

        builder.Services.TryAddSingleton<IJobHistoryStore, SqlServerJobHistoryStore>();

        builder.Services.AddOptions<JobSystemOptions>()
            .Configure(options =>
            {
                var sqlOptions = new SqlServerOptions();
                configure(sqlOptions);
                options.PollingFrequency = sqlOptions.PollingFrequency;
            });
        
        return builder;
    }
}