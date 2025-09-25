using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NOBS.JobSystem.Abstractions;

namespace NOBS.JobSystem.Execution;

internal class JobOrchestrator(
    IServiceProvider serviceProvider,
    IJobHistoryStore historyStore,
    JobRegistry jobRegistry,
    ILogger<JobOrchestrator> logger)
{
    private enum JobCompletionState
    {
        Success,
        Failure,
        UnhandledException
    }

    public async Task RunScheduledJobsAsync(CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var dueJobs = await GetDueJobsAsync(now, cancellationToken).ConfigureAwait(false);

        if (dueJobs.Count == 0)
        {
            logger.LogTrace("No scheduled jobs are due to run at {Now}", now);
            return;
        }

        var jobQueue = new Queue<JobConfiguration>(dueJobs);
        await ProcessJobQueueAsync(jobQueue, isScheduledRun: true, cancellationToken).ConfigureAwait(false);
        logger.LogInformation("Scheduled job check cycle finished at {Now}", now);
    }

    public async Task RunTriggeredJobAsync(string jobName, CancellationToken cancellationToken)
    {
        var config = jobRegistry.FindByName(jobName);
        if (config is null)
        {
            logger.LogError("A triggered job with name '{JobName}' was not found in the registry.", jobName);
            return;
        }

        var jobQueue = new Queue<JobConfiguration>();
        jobQueue.Enqueue(config);
        
        await ProcessJobQueueAsync(jobQueue, isScheduledRun: false, cancellationToken).ConfigureAwait(false);
        logger.LogInformation("Triggered job run for '{JobName}' finished.", jobName);
    }
    
    private async Task ProcessJobQueueAsync(Queue<JobConfiguration> jobQueue, bool isScheduledRun, CancellationToken cancellationToken)
    {
        while (jobQueue.Count > 0 && !cancellationToken.IsCancellationRequested)
        {
            var config = jobQueue.Dequeue();
            await ProcessJobAsync(config, jobQueue, isScheduledRun, cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task<List<JobConfiguration>> GetDueJobsAsync(DateTime now, CancellationToken cancellationToken)
    {
        var scheduledJobs = jobRegistry.JobConfigurations.Where(c => c.Schedule is not null).ToList();
        var jobNames = scheduledJobs.Select(c => c.Name);
        
        var lastRunTimes = await historyStore.GetLastRunTimesAsync(jobNames, cancellationToken).ConfigureAwait(false);

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

    private async Task ProcessJobAsync(JobConfiguration config, Queue<JobConfiguration> continuationQueue, bool isScheduledRun, CancellationToken cancellationToken)
    {
        logger.LogInformation("Executing job: {JobName}", config.Name);

        await using var scope = serviceProvider.CreateAsyncScope();
        if (scope.ServiceProvider.GetService(config.JobType) is not IJob job)
        {
            logger.LogError("Failed to resolve job '{JobName}' from DI container. Ensure it is registered.", config.Name);
            return;
        }

        var stopwatch = Stopwatch.StartNew();
        JobExecutionResult? result = null;
        Exception? exception = null;

        try
        {
            result = await job.ExecuteAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            exception = ex;
        }
        finally
        {
            stopwatch.Stop();
            await HandleJobCompletionAsync(config, result, exception, stopwatch.Elapsed, continuationQueue, isScheduledRun, cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task HandleJobCompletionAsync(
        JobConfiguration config,
        JobExecutionResult? result,
        Exception? exception,
        TimeSpan elapsed,
        Queue<JobConfiguration> continuationQueue,
        bool isScheduledRun,
        CancellationToken cancellationToken)
    {
        (JobCompletionState state, Type? nextJobType) = (exception, result) switch
        {
            ({} _, _) => (JobCompletionState.UnhandledException, config.ErrorJobType),
            (_, { Succeeded: true } r) => (JobCompletionState.Success, r.NextJobTypeOnSuccess),
            var (_, r) => (JobCompletionState.Failure, config.ErrorJobType ?? r?.NextJobTypeOnError)
        };

        switch (state)
        {
            case JobCompletionState.Success:
                logger.LogInformation("Job {JobName} completed successfully in {ElapsedMilliseconds}ms.", config.Name, elapsed.TotalMilliseconds);
                if (isScheduledRun && config.Schedule is not null)
                {
                    await historyStore.SetLastSuccessfulRunAsync(config.Name, DateTime.UtcNow, cancellationToken).ConfigureAwait(false);
                }
                break;
            case JobCompletionState.Failure:
                logger.LogWarning("Job {JobName} completed with a failure status in {ElapsedMilliseconds}ms.", config.Name, elapsed.TotalMilliseconds);
                break;
            case JobCompletionState.UnhandledException:
                logger.LogError(exception, "Job {JobName} failed with an unhandled exception after {ElapsedMilliseconds}ms.", config.Name, elapsed.TotalMilliseconds);
                break;
        }

        if (nextJobType is null)
        {
            return;
        }

        var continuationJob = jobRegistry.FindByType(nextJobType);
        if (continuationJob is null)
        {
            logger.LogError("Continuation job of type '{JobType}' was not registered and will not be executed.", nextJobType.FullName);
            return;
        }
        
        logger.LogInformation("Queueing continuation/error job: {JobName}", continuationJob.Name);
        continuationQueue.Enqueue(continuationJob);
    }
}