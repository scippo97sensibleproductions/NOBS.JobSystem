using System.ComponentModel.DataAnnotations;

namespace NOBS.JobSystem.Persistence.Entities;

/// <summary>
/// Represents the persisted record of a job's last successful execution.
/// </summary>
public sealed class JobExecutionHistory
{
    /// <summary>
    /// The full name of the job type.
    /// </summary>
    [Key]
    public string JobName { get; set; } = string.Empty;

    /// <summary>
    /// The UTC timestamp of the last successful run.
    /// </summary>
    public DateTime LastSuccessfulRun { get; set; }
}