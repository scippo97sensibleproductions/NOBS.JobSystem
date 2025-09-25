using System.Text.Json;
using Microsoft.Extensions.Options;
using NOBS.JobSystem.Abstractions;

namespace NOBS.JobSystem.Stores.JsonFile;

internal sealed class JsonFileJobHistoryStore(IOptions<JsonFileOptions> options) : IJobHistoryStore, IAsyncDisposable
{
    private readonly JsonFileOptions _options = options.Value;
    private readonly SemaphoreSlim _semaphore = new(1, 1);

    public Task InitializeAsync(CancellationToken cancellationToken)
    {
        var directory = Path.GetDirectoryName(_options.FilePath);
        if (directory is not null && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        if (!File.Exists(_options.FilePath))
        {
            return File.WriteAllTextAsync(_options.FilePath, "{}", cancellationToken);
        }
        
        return Task.CompletedTask;
    }

    public async Task<IReadOnlyDictionary<string, DateTime>> GetLastRunTimesAsync(IEnumerable<string> jobNames, CancellationToken cancellationToken)
    {
        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            var histories = await ReadHistoriesFromFileAsync(cancellationToken);
            return histories.Where(h => jobNames.Contains(h.Key))
                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task SetLastSuccessfulRunAsync(string jobName, DateTime lastSuccessfulRun, CancellationToken cancellationToken)
    {
        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            var histories = await ReadHistoriesFromFileAsync(cancellationToken);
            histories[jobName] = lastSuccessfulRun;
            await WriteHistoriesToFileAsync(histories, cancellationToken);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    private async Task<Dictionary<string, DateTime>> ReadHistoriesFromFileAsync(CancellationToken cancellationToken)
    {
        if (!File.Exists(_options.FilePath))
        {
            return [];
        }

        var json = await File.ReadAllTextAsync(_options.FilePath, cancellationToken);
        if (string.IsNullOrWhiteSpace(json))
        {
            return [];
        }
        
        return JsonSerializer.Deserialize<Dictionary<string, DateTime>>(json) ?? [];
    }

    private async Task WriteHistoriesToFileAsync(IReadOnlyDictionary<string, DateTime> histories, CancellationToken cancellationToken)
    {
        var json = JsonSerializer.Serialize(histories, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(_options.FilePath, json, cancellationToken);
    }

    public async ValueTask DisposeAsync()
    {
        await _semaphore.WaitAsync();
        _semaphore.Release();
        _semaphore.Dispose();
    }
}