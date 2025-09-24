using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace NOBS.JobSystem.Execution;

internal class ScheduledJobService(
    JobOrchestrator orchestrator,
    JobSystemOptions options,
    ILogger<ScheduledJobService> logger) : BackgroundService
{
    private readonly PeriodicTimer _timer = new(options.PollingFrequency);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Scheduled Job Service is starting.");

        while (await _timer.WaitForNextTickAsync(stoppingToken).ConfigureAwait(false) && !stoppingToken.IsCancellationRequested)
        {
            try
            {
                logger.LogInformation("Scheduled Job Service is checking for due jobs.");
                await orchestrator.RunScheduledJobs(stoppingToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                logger.LogCritical(ex, "An unhandled exception occurred during the job scheduling cycle. The service will continue.");
            }
        }

        logger.LogInformation("Scheduled Job Service is stopping.");
    }
}