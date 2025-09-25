using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using NOBS.JobSystem.Abstractions;
using NOBS.JobSystem.Stores.SqlServer.Persistence;
using NOBS.JobSystem.Stores.SqlServer.Persistence.Entities;

namespace NOBS.JobSystem.Stores.SqlServer;

internal sealed class SqlServerJobHistoryStore(
    IDbContextFactory<JobDbContext> dbContextFactory,
    IOptions<SqlServerOptions> options) : IJobHistoryStore
{
    private readonly SqlServerOptions _options = options.Value;
    
    public async Task InitializeAsync(CancellationToken cancellationToken)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        
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
                           [LastSuccessfulRun] datetimeoffset(7) NOT NULL,
                           CONSTRAINT [{pkName}] PRIMARY KEY CLUSTERED ([JobName] ASC)
                       );
                   END
                   """;

        await dbContext.Database.ExecuteSqlRawAsync(sql, cancellationToken).ConfigureAwait(false);
    }
    
    public async Task<IReadOnlyDictionary<string, DateTimeOffset>> GetLastRunTimesAsync(IEnumerable<string> jobNames, CancellationToken cancellationToken)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        
        return await dbContext.JobExecutionHistories
            .AsNoTracking()
            .Where(h => jobNames.Contains(h.JobName))
            .ToDictionaryAsync(h => h.JobName, h => h.LastSuccessfulRun, cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task SetLastSuccessfulRunAsync(string jobName, DateTimeOffset lastSuccessfulRun, CancellationToken cancellationToken)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var historyRecord = await dbContext.JobExecutionHistories.FindAsync([jobName], cancellationToken)
                            ?? dbContext.JobExecutionHistories.Add(new JobExecutionHistory { JobName = jobName }).Entity;

        historyRecord.LastSuccessfulRun = lastSuccessfulRun;
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}