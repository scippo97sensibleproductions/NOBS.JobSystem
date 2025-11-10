using System.ComponentModel.DataAnnotations;

namespace NOBS.JobSystem.Stores.MongoDb;

/// <summary>
/// Defines configuration options for the MongoDB persistence provider.
/// </summary>
public sealed class MongoDbOptions
{
    /// <summary>
    /// Gets or sets the MongoDB connection string.
    /// </summary>
    [Required(AllowEmptyStrings = false)]
    public string ConnectionString { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the name of the database to use.
    /// </summary>
    [Required(AllowEmptyStrings = false)]
    public string DatabaseName { get; set; } = "JobSystem";

    /// <summary>
    /// Gets or sets the name of the collection for storing job history.
    /// </summary>
    [Required(AllowEmptyStrings = false)]
    public string CollectionName { get; set; } = "ExecutionHistory";

    /// <summary>
    /// Gets or sets the frequency at which the system checks for due jobs.
    /// This overrides the default polling frequency in the core <see cref="JobSystemOptions"/>.
    /// </summary>
    public TimeSpan PollingFrequency { get; set; } = TimeSpan.FromMinutes(1);
}