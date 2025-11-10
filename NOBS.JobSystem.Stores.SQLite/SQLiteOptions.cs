using System.ComponentModel.DataAnnotations;

namespace NOBS.JobSystem.Stores.SQLite;

/// <summary>
/// Defines configuration options for the SQLite persistence provider.
/// </summary>
public sealed class SQLiteOptions
{
    /// <summary>
    /// Gets or sets the SQLite connection string.
    /// </summary>
    [Required(AllowEmptyStrings = false)]
    public string ConnectionString { get; set; } = "Data Source=jobs.db";

    /// <summary>
    /// Gets or sets the name of the table for storing job history.
    /// </summary>
    [Required(AllowEmptyStrings = false)]
    public string HistoryTableName { get; set; } = "ExecutionHistory";

    /// <summary>
    /// Gets or sets the frequency at which the system checks for due jobs.
    /// This overrides the default polling frequency in the core <see cref="JobSystemOptions"/>.
    /// </summary>
    public TimeSpan PollingFrequency { get; set; } = TimeSpan.FromMinutes(1);
}