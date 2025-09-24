using Microsoft.EntityFrameworkCore;
using NOBS.JobSystem.Execution;
using NOBS.JobSystem.Persistence;
using NOBS.JobSystem.UI.Models;

namespace NOBS.JobSystem.UI.Services;

/// <summary>
/// A service for retrieving the status of all registered jobs.
/// </summary>
internal class JobStatusService(
    JobRegistry jobRegistry,
    IDbContextFactory<JobDbContext> dbContextFactory)
{
    /// <summary>
    /// Gets the current status of all scheduled jobs.
    /// </summary>
    /// <param name="ct">A cancellation token.</param>
    /// <returns>A list of job statuses.</returns>
    public async Task<List<JobStatusDto>> GetJobStatusesAsync(CancellationToken ct = default)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

        var jobNames = jobRegistry.JobConfigurations
            .Select(c => c.Name)
            .ToList();

        var histories = await dbContext.JobExecutionHistories
            .AsNoTracking()
            .Where(h => jobNames.Contains(h.JobName))
            .ToDictionaryAsync(h => h.JobName, h => (DateTime?)h.LastSuccessfulRun, ct)
            .ConfigureAwait(false);

        var now = DateTime.UtcNow;

        return jobRegistry.JobConfigurations
            .Select(config => new JobStatusDto(config.Name, config.CronExpression ?? "N/A")
            {
                LastRunUtc = histories.GetValueOrDefault(config.Name),
                NextRunUtc = config.Schedule?.GetNextOccurrence(now)
            })
            .OrderBy(s => s.JobName)
            .ToList();
    }
}