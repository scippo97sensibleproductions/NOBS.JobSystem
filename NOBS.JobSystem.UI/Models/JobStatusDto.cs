namespace NOBS.JobSystem.UI.Models;

public sealed class JobStatusDto
{
    public required string JobName { get; set; }
    public required string CronExpression { get; set; }
    public DateTime? LastRunUtc { get; set; }
    public DateTime? NextRunUtc { get; set; }
}