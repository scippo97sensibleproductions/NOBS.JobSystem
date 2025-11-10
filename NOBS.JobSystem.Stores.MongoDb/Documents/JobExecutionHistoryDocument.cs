using MongoDB.Bson.Serialization.Attributes;

namespace NOBS.JobSystem.Stores.MongoDb.Documents;

internal sealed class JobExecutionHistoryDocument
{
    [BsonId]
    public string JobName { get; set; } = string.Empty;

    [BsonElement("lastSuccessfulRun")]
    public DateTimeOffset LastSuccessfulRun { get; set; }
}