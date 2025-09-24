using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using NOBS.JobSystem.Execution;
using NOBS.JobSystem.Persistence.Entities;

namespace NOBS.JobSystem.Persistence;

internal sealed class JobDbContext(DbContextOptions<JobDbContext> options, IOptions<JobSystemOptions> jobSystemOptions) : DbContext(options)
{
    private readonly JobSystemOptions _jobSystemOptions = jobSystemOptions.Value;

    public DbSet<JobExecutionHistory> JobExecutionHistories { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.Entity<JobExecutionHistory>().ToTable(_jobSystemOptions.HistoryTableName, _jobSystemOptions.SchemaName);
    }
}