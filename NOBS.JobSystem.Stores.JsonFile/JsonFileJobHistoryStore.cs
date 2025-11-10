using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using Microsoft.Extensions.Options;
using NOBS.JobSystem.Abstractions;

namespace NOBS.JobSystem.Stores.JsonFile;

internal sealed class JsonFileJobHistoryStore(
    IOptions<JsonFileOptions> options,
    JsonTypeInfo<Dictionary<string, DateTimeOffset>> historyTypeInfo)
    : IJobHistoryStore, IAsyncDisposable
{
    private readonly JsonFileOptions _options = options.Value;
    private readonly SemaphoreSlim _semaphore = new(1, 1);

    public Task InitializeAsync(CancellationToken cancellationToken)
    {
        var directory = Path.GetDirectoryName(_options.FilePath);
        if (!string.IsNullOrWhiteSpace(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        if (!File.Exists(_options.FilePath))
        {
            return File.WriteAllTextAsync(_options.FilePath, "{}", cancellationToken);
        }
        
        return Task.CompletedTask;
    }

    public async Task<IReadOnlyDictionary<string, DateTimeOffset>> GetLastRunTimesAsync(IEnumerable<string> jobNames, CancellationToken cancellationToken)
    {
        await _semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var histories = await ReadHistoriesFromFileAsync(cancellationToken).ConfigureAwait(false);
            return histories.Where(h => jobNames.Contains(h.Key))
                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task SetLastSuccessfulRunAsync(string jobName, DateTimeOffset lastSuccessfulRun, CancellationToken cancellationToken)
    {
        await _semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var histories = await ReadHistoriesFromFileAsync(cancellationToken).ConfigureAwait(false);
            histories[jobName] = lastSuccessfulRun;
            await WriteHistoriesToFileAsync(histories, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    private async Task<Dictionary<string, DateTimeOffset>> ReadHistoriesFromFileAsync(CancellationToken cancellationToken)
    {
        if (!File.Exists(_options.FilePath)) return [];

        await using var stream = File.OpenRead(_options.FilePath);
        if (stream.Length == 0) return [];

        return (await JsonSerializer.DeserializeAsync(stream, historyTypeInfo, cancellationToken).ConfigureAwait(false)) ?? [];
    }

    private async Task WriteHistoriesToFileAsync(IReadOnlyDictionary<string, DateTimeOffset> histories, CancellationToken cancellationToken)
    {
        var concreteHistories = histories is Dictionary<string, DateTimeOffset> h 
            ? h 
            : new Dictionary<string, DateTimeOffset>(histories);

        await using var stream = File.Create(_options.FilePath);
        await JsonSerializer.SerializeAsync(stream, concreteHistories, historyTypeInfo, cancellationToken).ConfigureAwait(false);
    }

    public async ValueTask DisposeAsync()
    {
        await _semaphore.WaitAsync().ConfigureAwait(false);
        _semaphore.Release();
        _semaphore.Dispose();
    }
}