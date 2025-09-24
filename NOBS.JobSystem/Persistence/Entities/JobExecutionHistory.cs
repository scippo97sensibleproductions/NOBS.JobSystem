using System.ComponentModel.DataAnnotations;

namespace NOBS.JobSystem.Persistence.Entities;

public sealed class JobExecutionHistory
{
    [Key]
    public string JobName { get; set; } = string.Empty;

    public DateTime LastSuccessfulRun { get; set; }
}