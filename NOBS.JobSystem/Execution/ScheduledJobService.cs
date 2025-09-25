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

        using var timer = new PeriodicTimer(pollingFrequency);

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var scheduledTask = timer.WaitForNextTickAsync(stoppingToken).AsTask();
                var triggeredTask = jobTrigger.GetTriggeredJobsAsync(stoppingToken).FirstOrDefaultAsync(stoppingToken).AsTask();

                var completedTask = await Task.WhenAny(scheduledTask, triggeredTask);

                if (completedTask == scheduledTask && scheduledTask.Result)
                {
                    await RunScheduledJobsAsync(stoppingToken);
                }
                else if (completedTask == triggeredTask && triggeredTask.Result is { } jobName)
                {
                    await RunTriggeredJobAsync(jobName, stoppingToken);
                }
            }
        }
        catch (OperationCanceledException)
        {
            logger.LogInformation("Scheduled Job Service is stopping due to cancellation.");
        }
        catch (Exception ex)
        {
            logger.LogCritical(ex, "A critical error occurred in the Scheduled Job Service. The service is stopping.");
        }

        logger.LogInformation("Scheduled Job Service has stopped.");
    }

    private async Task RunScheduledJobsAsync(CancellationToken stoppingToken)
    {
        try
        {
            logger.LogInformation("Scheduled Job Service is checking for due jobs.");
            await orchestrator.RunScheduledJobsAsync(stoppingToken).ConfigureAwait(false);
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
            await orchestrator.RunTriggeredJobAsync(jobName, stoppingToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An unhandled exception occurred during the triggered job run for {JobName}.", jobName);
        }
    }
}