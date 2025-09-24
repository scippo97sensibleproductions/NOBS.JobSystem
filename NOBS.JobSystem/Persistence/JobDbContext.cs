using Microsoft.EntityFrameworkCore;
using NOBS.JobSystem.Execution;
using NOBS.JobSystem.Persistence.Entities;

namespace NOBS.JobSystem.Persistence;

public sealed class JobDbContext(DbContextOptions<JobDbContext> options, JobSystemOptions jobSystemOptions) : DbContext(options)
{
    public DbSet<JobExecutionHistory> JobExecutionHistories { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.Entity<JobExecutionHistory>().ToTable(jobSystemOptions.HistoryTableName, jobSystemOptions.SchemaName);
    }
}