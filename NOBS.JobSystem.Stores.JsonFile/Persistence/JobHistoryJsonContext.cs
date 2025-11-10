using System.Text.Json.Serialization;

namespace NOBS.JobSystem.Stores.JsonFile.Persistence;

[JsonSerializable(typeof(Dictionary<string, DateTimeOffset>))]
internal partial class JobHistoryJsonContext : JsonSerializerContext
{
}