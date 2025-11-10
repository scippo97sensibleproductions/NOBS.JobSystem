using Microsoft.Extensions.Options;
using MongoDB.Driver;
using NOBS.JobSystem.Abstractions;
using NOBS.JobSystem.Stores.MongoDb.Documents;

namespace NOBS.JobSystem.Stores.MongoDb;

internal sealed class MongoDbJobHistoryStore : IJobHistoryStore
{
    private readonly IMongoCollection<JobExecutionHistoryDocument> _collection;

    public MongoDbJobHistoryStore(IOptions<MongoDbOptions> options)
    {
        var settings = options.Value;
        var mongoClient = new MongoClient(settings.ConnectionString);
        var mongoDatabase = mongoClient.GetDatabase(settings.DatabaseName);
        _collection = mongoDatabase.GetCollection<JobExecutionHistoryDocument>(settings.CollectionName);
    }

    public Task InitializeAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    public async Task<IReadOnlyDictionary<string, DateTimeOffset>> GetLastRunTimesAsync(IEnumerable<string> jobNames, CancellationToken cancellationToken)
    {
        var filter = Builders<JobExecutionHistoryDocument>.Filter.In(doc => doc.JobName, jobNames);

        var histories = await _collection.Find(filter)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return histories.ToDictionary(h => h.JobName, h => h.LastSuccessfulRun);
    }

    public async Task SetLastSuccessfulRunAsync(string jobName, DateTimeOffset lastSuccessfulRun, CancellationToken cancellationToken)
    {
        var document = new JobExecutionHistoryDocument
        {
            JobName = jobName,
            LastSuccessfulRun = lastSuccessfulRun
        };

        var filter = Builders<JobExecutionHistoryDocument>.Filter.Eq(doc => doc.JobName, jobName);
        var options = new ReplaceOptions { IsUpsert = true };

        await _collection.ReplaceOneAsync(filter, document, options, cancellationToken).ConfigureAwait(false);
    }
}