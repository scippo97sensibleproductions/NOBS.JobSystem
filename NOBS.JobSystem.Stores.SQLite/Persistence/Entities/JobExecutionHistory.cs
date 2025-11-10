using System.ComponentModel.DataAnnotations;

namespace NOBS.JobSystem.Stores.SQLite.Persistence.Entities;

internal sealed class JobExecutionHistory
{
    [Key]
    public string JobName { get; set; } = string.Empty;

    public DateTimeOffset LastSuccessfulRun { get; set; }
}