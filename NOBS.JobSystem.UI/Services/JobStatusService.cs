using NOBS.JobSystem.Abstractions;
using NOBS.JobSystem.Execution;
using NOBS.JobSystem.UI.Models;

namespace NOBS.JobSystem.UI.Services;

internal class JobStatusService(
    JobRegistry jobRegistry,
    IJobHistoryStore historyStore)
{
    public async Task<List<JobStatusDto>> GetJobStatusesAsync(CancellationToken ct = default)
    {
        var jobNames = jobRegistry.JobConfigurations
            .Where(c => c.Schedule is not null)
            .Select(c => c.Name)
            .ToList();

        if (jobNames.Count == 0)
        {
            return [];
        }

        var histories = await historyStore.GetLastRunTimesAsync(jobNames, ct).ConfigureAwait(false);
        var now = DateTime.UtcNow;

        return jobRegistry.JobConfigurations
            .Where(c => c.Schedule is not null)
            .Select(config =>
            {
                histories.TryGetValue(config.Name, out var lastRun);
                return new JobStatusDto(config.Name, config.CronExpression ?? "N/A")
                {
                    LastRunUtc = lastRun == DateTime.MinValue ? null : lastRun,
                    NextRunUtc = config.Schedule?.GetNextOccurrence(lastRun == DateTime.MinValue ? now : lastRun)
                };
            })
            .OrderBy(s => s.JobName)
            .ToList();
    }
}