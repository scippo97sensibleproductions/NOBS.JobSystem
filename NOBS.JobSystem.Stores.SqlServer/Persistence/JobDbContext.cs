using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using NOBS.JobSystem.Stores.SqlServer.Persistence.Entities;

namespace NOBS.JobSystem.Stores.SqlServer.Persistence;

internal sealed class JobDbContext(
    DbContextOptions<JobDbContext> options, 
    IOptions<SqlServerOptions> jobSystemOptions) : DbContext(options)
{
    private readonly SqlServerOptions _jobSystemOptions = jobSystemOptions.Value;

    public DbSet<JobExecutionHistory> JobExecutionHistories { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.Entity<JobExecutionHistory>().ToTable(_jobSystemOptions.HistoryTableName, _jobSystemOptions.SchemaName);
    }
}