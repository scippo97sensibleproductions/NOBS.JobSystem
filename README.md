# NOBS.JobSystem

A lightweight, persistence-agnostic, dependency-injection friendly job scheduling system for .NET applications. It provides CRON-based scheduling, job chaining, manual triggering, and a persistent execution history with an optional Blazor-based monitoring UI.

## Key Features

- **Fluent Configuration:** A clean, expressive API for registering jobs and their dependencies.
- **Hybrid Configuration:** Define job schedules and storage settings in `appsettings.json` while keeping job logic in code.
- **Pluggable Storage:** Persist job history to SQL Server, MongoDB, SQLite, JSON files, or a custom provider.
- **Stable Job Identity:** Use the `[JobName]` attribute to assign a persistent identifier to your jobs, preventing history loss when you refactor class names.
- **CRON Scheduling:** Define recurring jobs using standard CRON expressions.
- **Job Chaining:** Create powerful workflows by specifying continuation jobs on success or failure.
- **Global Error Handling:** Configure a specific job to run whenever any unhandled exception occurs.
- **DI-First Design:** Jobs are resolved from the service container, giving them full access to all registered application services.
- **Trimming and AOT Friendly:** Designed to work out-of-the-box with trimmed and AOT-compiled applications.
- **Manual Triggering:** Force any registered job to run immediately via the monitoring UI or programmatically.
- **Optional Monitoring UI:** A clean, lightweight Blazor UI to monitor job statuses.

## Packages

The system is distributed across multiple NuGet packages for modularity:

| Package                             | Description                                                                  |
| ----------------------------------- | ---------------------------------------------------------------------------- |
| `NOBS.JobSystem`                    | The core library containing the job orchestrator and scheduling logic.       |
| `NOBS.JobSystem.Hosting`            | A meta-package for easily installing and configuring the UI endpoint.        |
| `NOBS.JobSystem.UI`                 | The Blazor-based monitoring UI. (Typically consumed via the Hosting package) |
| `NOBS.JobSystem.Stores.SqlServer`   | Persistence provider for Microsoft SQL Server.                               |
| `NOBS.JobSystem.Stores.MongoDb`     | Persistence provider for MongoDB.                                            |
| `NOBS.JobSystem.Stores.SQLite`      | Persistence provider for SQLite.                                             |
| `NOBS.JobSystem.Stores.JsonFile`    | Persistence provider for a local JSON file.                                  |

## Usage Guide

### 1. Configuration via AppSettings

You can manage your job schedules and storage settings externally using `appsettings.json`. This allows you to change schedules without recompiling the application.

**appsettings.json**
```json
{
  "JobSystem": {
    "PollingFrequency": "00:01:00",
    "Jobs": {
      "report-processor": "0 2 * * *", 
      "db-cleanup": "0 4 * * 0"
    },
    "Storage": {
      "ConnectionString": "Server=.;Database=MyJobDb;Trusted_Connection=True;TrustServerCertificate=True;",
      "SchemaName": "scheduler",
      "HistoryTableName": "JobHistory",
      "PollingFrequency": "00:00:30"
    }
  }
}
```

**Program.cs**
```csharp
using NOBS.JobSystem.Execution;
using NOBS.JobSystem.Stores.SqlServer;

var builder = WebApplication.CreateBuilder(args);

// Register Jobs
builder.Services.AddScoped<ProcessDailyReportsJob>();
builder.Services.AddScoped<DatabaseCleanupJob>();

// Add Job System with Configuration
builder.Services
    .AddJobSystem(builder.Configuration.GetSection("JobSystem"), registry =>
    {
        // Schedules defined in appsettings.json will override the code defaults.
        registry.AddJob<ProcessDailyReportsJob>("0 1 * * *");
        registry.AddJob<DatabaseCleanupJob>(); 
    })
    .UseSqlServer(builder.Configuration.GetSection("JobSystem:Storage"));
```

### 2. Fluent Configuration

Alternatively, you can configure everything in code.

```csharp
builder.Services
    .AddJobSystem(registry =>
    {
        registry.AddJob<ProcessDailyReportsJob>("0 1 * * *")
            .OnSuccess<ArchiveOldReportsJob>()
            .OnError<ReportGenerationFailedJob>();

        registry.AddJob<ArchiveOldReportsJob>();
        registry.AddJob<ReportGenerationFailedJob>();
    })
    .UseJsonFile(options =>
    {
        options.FilePath = "Data/job_history.json"; 
        options.PollingFrequency = TimeSpan.FromSeconds(30);
    });
```

## Defining Jobs

Jobs are simple classes that implement `IJob`.

```csharp
[JobName("report-processor")]
public class ProcessDailyReportsJob(ILogger<ProcessDailyReportsJob> logger) : IJob
{
    public async Task<JobExecutionResult> ExecuteAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Starting daily report generation...");
        // Logic...
        return JobExecutionResult.Success();
    }
}
```

## Building From Source

To build the project from source, clone the repository and run the .NET build command from the root directory.

```shell
git clone https://github.com/scippo97sensibleproductions/NOBS.JobSystem.git
cd NOBS.JobSystem
dotnet build -c Release
```

## Contributing

Contributions are welcome. Please follow these standard steps:

1.  Fork the repository.
2.  Create a new feature branch (`git checkout -b feature/your-feature-name`).
3.  Commit your changes (`git commit -m 'Add some feature'`).
4.  Push to the branch (`git push origin feature/your-feature-name`).
5.  Open a pull request.

## License

This project is licensed under the MIT License. See the [LICENSE](LICENSE) file for details.