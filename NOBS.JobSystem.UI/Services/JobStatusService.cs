// FILE: C:\Users\WEPLUSVMADM\RiderProjects\NOBSJobSystem\NOBS.JobSystem.UI\Services\JobStatusService.cs
using Microsoft.EntityFrameworkCore;
using NOBS.JobSystem.Execution;
using NOBS.JobSystem.Persistence;
using NOBS.JobSystem.UI.Models;

namespace NOBS.JobSystem.UI.Services;

public class JobStatusService(
    JobRegistry jobRegistry,
    IDbContextFactory<JobDbContext> dbContextFactory)
{
    public async Task<List<JobStatusDto>> GetJobStatusesAsync(CancellationToken ct = default)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(ct);

        var jobNames = jobRegistry.JobConfigurations
            .Select(c => c.Name)
            .ToList();

        var histories = await dbContext.JobExecutionHistories
            .AsNoTracking()
            .Where(h => jobNames.Contains(h.JobName))
            .ToDictionaryAsync(h => h.JobName, h => (DateTime?)h.LastSuccessfulRun, ct);

        var now = DateTime.UtcNow;

        return jobRegistry.JobConfigurations
            .Select(config => new JobStatusDto
            {
                JobName = config.Name,
                CronExpression = config.CronExpression ?? "N/A",
                LastRunUtc = histories.GetValueOrDefault(config.Name),
                NextRunUtc = config.Schedule?.GetNextOccurrence(now)
            })
            .OrderBy(s => s.JobName)
            .ToList();
    }
}