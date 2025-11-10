# NOBS.JobSystem

A lightweight, persistence-agnostic, dependency-injection friendly job scheduling system for .NET applications. It provides CRON-based scheduling, job chaining, manual triggering, and a persistent execution history with an optional Blazor-based monitoring UI.

## Key Features

- **Fluent Configuration:** A clean, expressive API for registering jobs and their dependencies.
- **Pluggable Storage:** Persist job history to SQL Server, MongoDB, SQLite, JSON files, or a custom provider. The core system is completely decoupled from the storage layer.
- **Stable Job Identity:** Use the `[JobName]` attribute to assign a persistent identifier to your jobs, preventing history loss when you refactor class names.
- **CRON Scheduling:** Define recurring jobs using standard CRON expressions (`minute hour day-of-month month day-of-week`).
- **Job Chaining:** Create powerful workflows by specifying continuation jobs on success or failure, either statically during registration or dynamically at runtime.
- **Global Error Handling:** Configure a specific job to run whenever any unhandled exception occurs.
- **DI-First Design:** Jobs are resolved from the service container, giving them full access to all registered application services.
- **Trimming and AOT Friendly:** Designed to work out-of-the-box with trimmed and AOT-compiled applications by default, with no complex configuration required.
- **Manual Triggering:** Force any registered job to run immediately via the monitoring UI or programmatically using the `IJobTrigger` service.
- **Optional Monitoring UI:** A clean, lightweight Blazor UI to monitor job statuses, last run times, and next scheduled runs.

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
| `NOBS.JobSystem.Stores.JsonFile`    | Persistence provider for a local JSON file (ideal for simple, single-instance apps). |

## Usage Guide

This guide demonstrates how to set up a multi-step job workflow:
1.  A scheduled job `ProcessDailyReportsJob` runs daily.
2.  On success, it chains to `ArchiveOldReportsJob`.
3.  On any failure, it chains to `ReportGenerationFailedJob`.

### 1. Install Packages

Install the core library, a storage provider, and the UI hosting package.

```shell
# Core packages
dotnet add package NOBS.JobSystem
dotnet add package NOBS.JobSystem.Hosting

# Choose ONE storage provider
dotnet add package NOBS.JobSystem.Stores.SqlServer
# or
dotnet add package NOBS.JobSystem.Stores.MongoDb
# or
dotnet add package NOBS.JobSystem.Stores.SQLite
# or
dotnet add package NOBS.JobSystem.Stores.JsonFile
```

### 2. Define Your Jobs

Jobs are simple classes that implement `IJob`. They can be injected with any service from your DI container.

**ProcessDailyReportsJob.cs**
```csharp
[JobName("report-processor")]
public class ProcessDailyReportsJob(ILogger<ProcessDailyReportsJob> logger, IReportGenerator reportGenerator) : IJob
{
    public async Task<JobExecutionResult> ExecuteAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Starting daily report generation...");
        bool success = await reportGenerator.GenerateAsync(cancellationToken);
        if (success) return JobExecutionResult.Success();
        return JobExecutionResult.Failure();
    }
}
```

**ArchiveOldReportsJob.cs**
```csharp
[JobName("report-archiver")]
public class ArchiveOldReportsJob(ILogger<ArchiveOldReportsJob> logger) : IJob
{
    public async Task<JobExecutionResult> ExecuteAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Archiving old reports...");
        await Task.Delay(500, cancellationToken);
        return JobExecutionResult.Success();
    }
}
```

**ReportGenerationFailedJob.cs**
```csharp
[JobName("report-failure-handler")]
public class ReportGenerationFailedJob(ILogger<ReportGenerationFailedJob> logger) : IJob
{
    public async Task<JobExecutionResult> ExecuteAsync(CancellationToken cancellationToken)
    {
        logger.LogError("Report generation failed. Notifying administrators.");
        await Task.CompletedTask;
        return JobExecutionResult.Success();
    }
}
```

### 3. Register Services and UI

In your `Program.cs`, configure the job system, register your jobs, and map the UI.

```csharp
using NOBS.JobSystem;
using NOBS.JobSystem.Execution;
using NOBS.JobSystem.Hosting;
using NOBS.JobSystem.Stores.JsonFile;
using NOBS.JobSystem.Stores.MongoDb;
using NOBS.JobSystem.Stores.SQLite;
using NOBS.JobSystem.Stores.SqlServer;

var builder = WebApplication.CreateBuilder(args);

// 1. Add application services used by your jobs
builder.Services.AddScoped<IReportGenerator, ReportGenerator>();

// 2. CRITICAL: Register all of your job types with the DI container.
// This is required for the system to find and execute them, especially in trimmed/AOT applications.
builder.Services.AddScoped<ProcessDailyReportsJob>();
builder.Services.AddScoped<ArchiveOldReportsJob>();
builder.Services.AddScoped<ReportGenerationFailedJob>();

// 3. Add the Job System
builder.Services
    .AddJobSystem(registry =>
    {
        // 4. Register jobs and define workflows
        registry.AddJob<ProcessDailyReportsJob>("0 1 * * *") // Run at 1:00 AM UTC daily
            .OnSuccess<ArchiveOldReportsJob>()
            .OnError<ReportGenerationFailedJob>();

        // Register continuation/error jobs without a schedule so the system is aware of them
        registry.AddJob<ArchiveOldReportsJob>();
        registry.AddJob<ReportGenerationFailedJob>();
    })
    // 5. Configure a storage provider (CHOOSE ONE)
    .UseJsonFile(options =>
    {
        // The path is relative to the application's content root.
        options.FilePath = "Data/job_history.json"; 
        options.PollingFrequency = TimeSpan.FromSeconds(30);
    });
    // Or, for SQL Server:
    // .UseSqlServer(options =>
    // {
    //     options.ConnectionString = builder.Configuration.GetConnectionString("JobDb");
    //     options.SchemaName = "jobs";
    //     options.PollingFrequency = TimeSpan.FromMinutes(1);
    // });
    // Or, for MongoDB:
    // .UseMongoDb(options =>
    // {
    //      options.ConnectionString = builder.Configuration.GetConnectionString("JobDbMongo");
    //      options.DatabaseName = "JobSystem";
    //      options.PollingFrequency = TimeSpan.FromMinutes(1);
    // });
    // Or, for SQLite:
    // .UseSQLite(options =>
    // {
    //      options.ConnectionString = "Data Source=jobs.db";
    //      options.PollingFrequency = TimeSpan.FromMinutes(1);
    // });

// 6. Add services for the Blazor UI
builder.Services.AddJobMonitorUI();

var app = builder.Build();

// ... other middleware

// 7. Map the Blazor UI and the trigger endpoint
app.MapHostedJobSystemUI();

app.Run();
```

## Advanced Usage

### Dynamic Job Chaining

Instead of defining the entire chain at registration, a job can dynamically decide which job to run next based on its internal logic.

```csharp
public class DynamicWorkflowJob : IJob
{
    public async Task<JobExecutionResult> ExecuteAsync(CancellationToken cancellationToken)
    {
        var condition = await CheckSomeConditionAsync();

        if (condition)
        {
            // Dynamically chain to HighPriorityContinuationJob
            return JobExecutionResult.Success(typeof(HighPriorityContinuationJob));
        }

        // Dynamically chain to LowPriorityContinuationJob
        return JobExecutionResult.Success(typeof(LowPriorityContinuationJob));
    }
}
```
*Note: `HighPriorityContinuationJob` and `LowPriorityContinuationJob` must still be registered with DI and the `JobRegistry` so the system is aware of them.*

### Stable Job Naming with `[JobName]`

Class names and namespaces can change during refactoring. To ensure the job's execution history is preserved, assign a stable, unique name with the `[JobName]` attribute. This name is used as the key in the persistence store, decoupling the job's identity from its type name.

```csharp
[JobName("process-customer-invoices-v1")]
public class ProcessInvoicesJob : IJob
{
    // ...
}
```

### Programmatic Triggering

Besides the UI, you can trigger a job from anywhere in your application by injecting `IJobTrigger`.

```csharp
public class SomeApiController(IJobTrigger jobTrigger) : ControllerBase
{
    [HttpPost("invoices/process-now")]
    public async Task<IActionResult> ProcessInvoicesNow()
    {
        // Use the stable name defined in the [JobName] attribute
        await jobTrigger.TriggerJobAsync("process-customer-invoices-v1");
        return Accepted();
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