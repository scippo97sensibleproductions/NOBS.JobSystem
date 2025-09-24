using System.ComponentModel.DataAnnotations;

namespace NOBS.JobSystem.Execution;

/// <summary>
/// Defines configuration options for the job system.
/// </summary>
public sealed class JobSystemOptions
{
    /// <summary>
    /// The database connection string for storing job execution history.
    /// </summary>
    [Required(AllowEmptyStrings = false)]
    public string ConnectionString { get; set; } = string.Empty;

    /// <summary>
    /// The database schema name to use for job system tables. Defaults to "jobs".
    /// </summary>
    [Required(AllowEmptyStrings = false)]
    public string SchemaName { get; set; } = "jobs";

    /// <summary>
    /// The table name for the job execution history. Defaults to "ExecutionHistory".
    /// </summary>
    [Required(AllowEmptyStrings = false)]
    public string HistoryTableName { get; set; } = "ExecutionHistory";

    /// <summary>
    /// The frequency at which the system checks for due jobs. Defaults to one minute.
    /// </summary>
    public TimeSpan PollingFrequency { get; set; } = TimeSpan.FromMinutes(1);
}