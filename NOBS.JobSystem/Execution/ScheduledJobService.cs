using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NOBS.JobSystem.Abstractions;

namespace NOBS.JobSystem.Execution;

internal class ScheduledJobService(
    JobOrchestrator orchestrator,
    IJobTrigger jobTrigger,
    TimeSpan pollingFrequency,
    ILogger<ScheduledJobService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Scheduled Job Service is starting.");

        var scheduledProcessingTask = ProcessScheduledJobsAsync(stoppingToken);
        var triggeredProcessingTask = ProcessTriggeredJobsAsync(stoppingToken);

        var completedTask = await Task.WhenAny(scheduledProcessingTask, triggeredProcessingTask);

        if (completedTask.IsFaulted)
        {
            logger.LogCritical(completedTask.Exception?.GetBaseException(), "A critical error occurred in the Scheduled Job Service. The service is stopping.");
        }

        logger.LogInformation("Scheduled Job Service has stopped.");
    }

    private async Task ProcessScheduledJobsAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(pollingFrequency);
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            await RunScheduledJobsAsync(stoppingToken);
        }
    }

    private async Task ProcessTriggeredJobsAsync(CancellationToken stoppingToken)
    {
        await foreach (var jobName in jobTrigger.GetTriggeredJobsAsync(stoppingToken))
        {
            _ = RunTriggeredJobAsync(jobName, stoppingToken);
        }
    }

    private async Task RunScheduledJobsAsync(CancellationToken stoppingToken)
    {
        try
        {
            logger.LogInformation("Scheduled Job Service is checking for due jobs.");
            await orchestrator.RunScheduledJobsAsync(stoppingToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An unhandled exception occurred during the scheduled job cycle. The service will continue.");
        }
    }

    private async Task RunTriggeredJobAsync(string jobName, CancellationToken stoppingToken)
    {
        try
        {
            logger.LogInformation("Executing manually triggered job: {JobName}", jobName);
            await orchestrator.RunTriggeredJobAsync(jobName, stoppingToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An unhandled exception occurred during the triggered job run for {JobName}.", jobName);
        }
    }
}