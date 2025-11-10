using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using NOBS.JobSystem.Stores.SQLite.Persistence.Entities;

namespace NOBS.JobSystem.Stores.SQLite.Persistence;

internal sealed class JobDbContext(
    DbContextOptions<JobDbContext> options,
    IOptions<SQLiteOptions> jobSystemOptions) : DbContext(options)
{
    private readonly SQLiteOptions _jobSystemOptions = jobSystemOptions.Value;

    public DbSet<JobExecutionHistory> JobExecutionHistories { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.Entity<JobExecutionHistory>().ToTable(_jobSystemOptions.HistoryTableName);
    }
}