using System.Threading.Channels;
using NOBS.JobSystem.Abstractions;

namespace NOBS.JobSystem.Execution;

internal sealed class InMemoryJobTrigger : IJobTrigger
{
    private readonly Channel<string> _channel = Channel.CreateUnbounded<string>(
        new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = false
        });

    public ValueTask TriggerJobAsync(string jobName) => 
        _channel.Writer.WriteAsync(jobName);

    public IAsyncEnumerable<string> GetTriggeredJobsAsync(CancellationToken cancellationToken) =>
        _channel.Reader.ReadAllAsync(cancellationToken);
}