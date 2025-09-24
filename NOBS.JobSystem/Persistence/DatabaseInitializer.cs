using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NOBS.JobSystem.Execution;

namespace NOBS.JobSystem.Persistence;

internal sealed class DatabaseInitializer(
    IServiceProvider serviceProvider,
    IOptions<JobSystemOptions> options,
    ILogger<DatabaseInitializer> logger) : IHostedService
{
    private readonly JobSystemOptions _options = options.Value;

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Job system database initializer is running.");

        await using var scope = serviceProvider.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<JobDbContext>();

        try
        {
            await EnsureSchemaAndTableExistAsync(dbContext, cancellationToken).ConfigureAwait(false);
            logger.LogInformation("Job system database schema is present and correct.");
        }
        catch (Exception ex)
        {
            logger.LogCritical(ex, "Failed to initialize the job system database schema.");
            throw;
        }
    }

    private async Task EnsureSchemaAndTableExistAsync(DbContext dbContext, CancellationToken cancellationToken)
    {
        var schemaName = _options.SchemaName;
        var tableName = _options.HistoryTableName;
        var pkName = $"PK_{tableName}";

        var sql = $"""
                   IF SCHEMA_ID(N'{schemaName}') IS NULL
                       EXEC(N'CREATE SCHEMA [{schemaName}]');

                   IF OBJECT_ID(N'[{schemaName}].[{tableName}]', N'U') IS NULL
                   BEGIN
                       CREATE TABLE [{schemaName}].[{tableName}] (
                           [JobName] nvarchar(450) NOT NULL,
                           [LastSuccessfulRun] datetime2(7) NOT NULL,
                           CONSTRAINT [{pkName}] PRIMARY KEY CLUSTERED ([JobName] ASC)
                       );
                   END
                   """;

        await dbContext.Database.ExecuteSqlRawAsync(sql, cancellationToken).ConfigureAwait(false);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}