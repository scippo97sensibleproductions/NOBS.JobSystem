using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NOBS.JobSystem.Abstractions;
using NOBS.JobSystem.Persistence;
using NOBS.JobSystem.Persistence.Entities;

namespace NOBS.JobSystem.Execution;

public class JobOrchestrator(
    IServiceProvider serviceProvider,
    IDbContextFactory<JobDbContext> dbContextFactory,
    JobRegistry jobRegistry,
    ILogger<JobOrchestrator> logger)
{
    public async Task RunScheduledJobs(CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var jobsToRun = await GetDueJobsAsync(now, cancellationToken);
        
        if (jobsToRun.Count == 0)
        {
            logger.LogTrace("No scheduled jobs are due to run at {Now}", now);
            return;
        }

        var jobQueue = new Queue<JobConfiguration>(jobsToRun);

        while (jobQueue.Count > 0 && !cancellationToken.IsCancellationRequested)
        {
            var config = jobQueue.Dequeue();
            await ProcessJobAsync(config, jobQueue, cancellationToken);
        }

        logger.LogInformation("Job check cycle finished at {Now}", now);
    }

    private async Task<List<JobConfiguration>> GetDueJobsAsync(DateTime now, CancellationToken cancellationToken)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        var scheduledJobs = jobRegistry.JobConfigurations.Where(c => c.Schedule is not null).ToList();
        var jobNames = scheduledJobs.Select(c => c.Name).ToList();

        var lastRunTimes = await dbContext.JobExecutionHistories
            .AsNoTracking()
            .Where(h => jobNames.Contains(h.JobName))
            .ToDictionaryAsync(h => h.JobName, h => h.LastSuccessfulRun, cancellationToken);

        var dueJobs = new List<JobConfiguration>();
        foreach (var config in scheduledJobs)
        {
            if (!lastRunTimes.TryGetValue(config.Name, out var lastRunUtc))
            {
                logger.LogInformation("Job '{JobName}' has no execution history. Queueing for immediate execution.", config.Name);
                dueJobs.Add(config);
                continue;
            }

            var nextOccurrenceUtc = config.Schedule!.GetNextOccurrence(lastRunUtc);

            if (nextOccurrenceUtc <= now)
            {
                logger.LogInformation("Job '{JobName}' has a pending execution scheduled for {NextOccurrenceUtc}. Queueing.", config.Name, nextOccurrenceUtc);
                dueJobs.Add(config);
            }
        }

        return dueJobs;
    }

    private async Task ProcessJobAsync(JobConfiguration config, Queue<JobConfiguration> continuationQueue, CancellationToken cancellationToken)
    {
        logger.LogInformation("Executing job: {JobName}", config.Name);

        await using var scope = serviceProvider.CreateAsyncScope();
        if (scope.ServiceProvider.GetService(config.JobType) is not IJob job)
        {
            logger.LogError("Failed to resolve job '{JobName}' from DI container.", config.Name);
            return;
        }

        var stopwatch = Stopwatch.StartNew();
        JobExecutionResult? result = null;
        Exception? exception = null;

        try
        {
            result = await job.ExecuteAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            exception = ex;
        }
        finally
        {
            stopwatch.Stop();
            await HandleJobCompletionAsync(config, result, exception, stopwatch.Elapsed, continuationQueue, cancellationToken);
        }
    }

    private async Task HandleJobCompletionAsync(
        JobConfiguration config, 
        JobExecutionResult? result, 
        Exception? exception, 
        TimeSpan elapsed, 
        Queue<JobConfiguration> continuationQueue,
        CancellationToken cancellationToken)
    {
        Type? nextJobType = null;

        if (exception is not null)
        {
            logger.LogError(exception, "Job {JobName} failed with an unhandled exception after {ElapsedMilliseconds} ms.", config.Name, elapsed.TotalMilliseconds);
            nextJobType = config.ErrorJobType;
        }
        else if (result?.Succeeded == true)
        {
            logger.LogInformation("Job {JobName} completed successfully in {ElapsedMilliseconds} ms.", config.Name, elapsed.TotalMilliseconds);
            if (config.Schedule is not null)
            {
                await UpdateJobHistoryAsync(config.Name, DateTime.UtcNow, cancellationToken);
            }
            nextJobType = result.NextJobTypeOnSuccess;
        }
        else
        {
            logger.LogWarning("Job {JobName} completed with a failure status in {ElapsedMilliseconds} ms.", config.Name, elapsed.TotalMilliseconds);
            nextJobType = config.ErrorJobType ?? result?.NextJobTypeOnError;
        }

        if (nextJobType is not null)
        {
            logger.LogInformation("Queueing continuation/error job: {JobName}", nextJobType.Name);
            continuationQueue.Enqueue(new JobConfiguration(nextJobType, null));
        }
    }

    private async Task UpdateJobHistoryAsync(string jobName, DateTime executionTime, CancellationToken ct)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(ct);
        var historyRecord = await dbContext.JobExecutionHistories.FindAsync([jobName], ct)
                            ?? dbContext.JobExecutionHistories.Add(new JobExecutionHistory { JobName = jobName }).Entity;

        historyRecord.LastSuccessfulRun = executionTime;
        await dbContext.SaveChangesAsync(ct);
    }
}