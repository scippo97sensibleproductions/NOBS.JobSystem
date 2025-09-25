using System.ComponentModel.DataAnnotations;

namespace NOBS.JobSystem.Stores.SqlServer;

public sealed class SqlServerOptions
{
    [Required(AllowEmptyStrings = false)]
    public string ConnectionString { get; set; } = string.Empty;

    [Required(AllowEmptyStrings = false)]
    public string SchemaName { get; set; } = "jobs";

    [Required(AllowEmptyStrings = false)]
    public string HistoryTableName { get; set; } = "ExecutionHistory";

    public TimeSpan PollingFrequency { get; set; } = TimeSpan.FromMinutes(1);
}