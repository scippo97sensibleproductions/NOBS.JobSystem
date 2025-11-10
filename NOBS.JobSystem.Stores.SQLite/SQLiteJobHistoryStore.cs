using Microsoft.EntityFrameworkCore;
using NOBS.JobSystem.Abstractions;
using NOBS.JobSystem.Stores.SQLite.Persistence;
using NOBS.JobSystem.Stores.SQLite.Persistence.Entities;

namespace NOBS.JobSystem.Stores.SQLite;

internal sealed class SQLiteJobHistoryStore(IDbContextFactory<JobDbContext> dbContextFactory) : IJobHistoryStore
{
    public async Task InitializeAsync(CancellationToken cancellationToken)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        await dbContext.Database.EnsureCreatedAsync(cancellationToken).ConfigureAwait(false);
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