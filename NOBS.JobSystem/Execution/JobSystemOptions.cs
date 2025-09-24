namespace NOBS.JobSystem.Execution;

public sealed class JobSystemOptions
{
    public string ConnectionString { get; set; } = string.Empty;
    public string SchemaName { get; set; } = "jobs";
    public string HistoryTableName { get; set; } = "ExecutionHistory";
    public TimeSpan PollingFrequency { get; set; } = TimeSpan.FromMinutes(1);
}