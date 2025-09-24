# NOBS.JobSystem

[![NuGet Version](https://img.shields.io/nuget/v/NOBS.JobSystem.svg?style=flat-square)](https://www.nuget.org/packages/NOBS.JobSystem/)
[![NuGet Version](https://img.shields.io/nuget/v/NOBS.JobSystem.UI.svg?style=flat-square)](https://www.nuget.org/packages/NOBS.JobSystem.UI/)
[![NuGet Version](https://img.shields.io/nuget/v/NOBS.JobSystem.Hosting.svg?style=flat-square)](https://www.nuget.org/packages/NOBS.JobSystem.Hosting/)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg?style=flat-square)](https://opensource.org/licenses/MIT)

A lightweight, database-backed, dependency-injection friendly job scheduling system for .NET applications. It provides CRON-based scheduling, job chaining, and a persistent execution history with an optional Blazor-based monitoring UI.

## Key Features

- **Stable Job Identity:** Use the `[JobName]` attribute to assign a persistent identifier to your jobs, preventing history loss when you refactor code.
- **CRON Scheduling:** Define recurring jobs using standard CRON expressions.
- **Job Chaining:** Create workflows by specifying continuation jobs on success or failure.
- **Persistent History:** Automatically tracks the last successful run time for each job in a database, ensuring schedules resume correctly after an application restart.
- **DI-First Design:** Jobs are resolved from the service container, giving them access to all registered application services.
- **Error Handling:** Configure specific jobs to run when a preceding job fails or throws an exception.
- **Optional Monitoring UI:** A clean, lightweight Blazor UI to monitor job statuses, last run times, and next scheduled runs.
- **Automatic Schema Creation:** The required database schema and table for storing job history are created automatically and idempotently on application startup.

## Packages

The system is distributed across three NuGet packages:

| Package                  | Description                                                                  |
| ------------------------ | ---------------------------------------------------------------------------- |
| `NOBS.JobSystem`         | The core library containing the job orchestrator and scheduling logic.       |
| `NOBS.JobSystem.UI`      | The Blazor-based monitoring UI.                                              |
| `NOBS.JobSystem.Hosting` | A meta-package for easily installing and configuring both the core and UI.   |

## Installation

Install the hosting package for the quickest setup in an ASP.NET Core application.

```shell
dotnet add package NOBS.JobSystem.Hosting
```

Or, install the packages individually if you do not require the UI.

```shell
dotnet add package NOBS.JobSystem
```

## Quick Start

### 1. Define Jobs with Stable Names

Create classes that implement the `IJob` interface. **It is strongly recommended to assign a stable, unique name to every job using the `[JobName]` attribute.**

```csharp
// src/MyWebApp/Jobs/HelloWorldJob.cs
using NOBS.JobSystem.Abstractions;

[JobName("hello-world-greeter")]
public class HelloWorldJob(ILogger<HelloWorldJob> logger) : IJob
{
    public Task<JobExecutionResult> ExecuteAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Hello from a scheduled job at {UtcNow}!", DateTime.UtcNow);
        return Task.FromResult(JobExecutionResult.Success());
    }
}
```

### 2. Configure the Job System

In your `Program.cs`, add the job system, configure the database connection, and register your jobs. **All jobs, including those used only for chaining, must be registered.**

```csharp
// src/MyWebApp/Program.cs
using NOBS.JobSystem.Hosting;

var builder = WebApplication.CreateBuilder(args);
var connectionString = builder.Configuration.GetConnectionString("JobDbConnection");

// Add services to the container.
builder.Services.AddHostedJobSystem(
    options =>
    {
        options.ConnectionString = connectionString;
    },
    registry =>
    {
        registry.AddJob<HelloWorldJob>("* * * * *");
    }
);

var app = builder.Build();

// Map the UI endpoint
app.MapHostedJobSystemUI();

app.Run();
```

### 3. Provide a Connection String

Ensure the connection string is present in your `appsettings.json`.

```json
{
  "ConnectionStrings": {
    "JobDbConnection": "Server=(localdb)\\mssqllocaldb;Database=MyJobSystemDb;Trusted_Connection=True;"
  }
}
```

The system will automatically create the necessary database schema and table on startup if they do not already exist.

## Advanced Usage

### Configuring System Options

The `JobSystemOptions` class provides several parameters to customize the system's behavior.

```csharp
// src/MyWebApp/Program.cs
builder.Services.AddHostedJobSystem(
    options =>
    {
        // REQUIRED: The database connection string for storing job history.
        options.ConnectionString = connectionString;

        // OPTIONAL: The frequency at which the system checks for due jobs.
        // Default is 1 minute.
        options.PollingFrequency = TimeSpan.FromSeconds(30);
        
        // OPTIONAL: The database schema to use for the history table.
        // Default is "jobs".
        options.SchemaName = "JobRunner";

        // OPTIONAL: The table name for the job execution history.
        // Default is "ExecutionHistory".
        options.HistoryTableName = "JobHistory";
    },
    registry => { /* ... */ }
);
```

### Advanced Workflows and Error Handling

The system provides two primary mechanisms for handling non-successful outcomes:
1.  **Declarative `.OnError<T>()`:** This handler is invoked **only** when a job's `ExecuteAsync` method throws an unhandled exception. It is ideal for global, unexpected failure conditions like a database connection being lost.
2.  **Imperative `JobExecutionResult.Failure(typeof(T))`:** This handler is invoked when a job completes its logic but determines a failure state (e.g., validation fails, an external API returns a non-200 status). It represents a controlled, expected failure path.

An unhandled exception will always trigger the `.OnError<T>` handler if it is configured, superseding any `Failure` result.

```csharp
// Job definitions
[JobName("data-fetch")]
public class DataFetchJob : IJob
{
    public async Task<JobExecutionResult> ExecuteAsync(CancellationToken cancellationToken)
    {
        var (isSuccess, hasData) = await FetchDataFromApiAsync();

        if (!isSuccess)
        {
            // Controlled failure path
            return JobExecutionResult.Failure(typeof(ApiDownAlertJob));
        }

        // Success path
        return JobExecutionResult.Success(typeof(ProcessDataJob));
    }
}

[JobName("data-process")]
public class ProcessDataJob : IJob { /* ... */ }

[JobName("api-down-alert")]
public class ApiDownAlertJob : IJob { /* ... */ }

[JobName("critical-failure-alert")]
public class CriticalFailureAlertJob : IJob { /* ... */ }


// In Program.cs
registry.AddJob<ProcessDataJob>();
registry.AddJob<ApiDownAlertJob>();
registry.AddJob<CriticalFailureAlertJob>();

registry.AddJob<DataFetchJob>("0 * * * *")      // Run hourly
    .OnError<CriticalFailureAlertJob>();       // Run if DataFetchJob throws an unhandled exception
```

### Leveraging Dependency Injection

Jobs are resolved from the service container, allowing for the injection of any registered application service.

```csharp
// A custom application service
public interface IReportingService
{
    Task<byte[]> GenerateMonthlyReportAsync(CancellationToken cancellationToken);
}

public class ReportingService : IReportingService 
{
    // ... implementation
}

// The job that uses the service
[JobName("monthly-report-generator")]
public class MonthlyReportJob(IReportingService reportingService, ILogger<MonthlyReportJob> logger) : IJob
{
    public async Task<JobExecutionResult> ExecuteAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Generating monthly report.");
        var reportBytes = await reportingService.GenerateMonthlyReportAsync(cancellationToken);
        // ... save or email report
        return JobExecutionResult.Success();
    }
}

// In Program.cs
var builder = WebApplication.CreateBuilder(args);

// Register your application services
builder.Services.AddScoped<IReportingService, ReportingService>();

builder.Services.AddHostedJobSystem(
    options => { /* ... */ },
    registry =>
    {
        // Register the job
        registry.AddJob<MonthlyReportJob>("0 0 1 * *"); // Run on the 1st of every month
    }
);
```

### Handling Cancellation

For long-running jobs, it is critical to respect the `CancellationToken` provided to `ExecuteAsync`. This ensures that jobs can be terminated gracefully when the application shuts down. Pass the token to any async methods that support it and periodically check its status in long loops.

```csharp
[JobName("long-running-task")]
public class LongRunningJob(ILogger<LongRunningJob> logger) : IJob
{
    public async Task<JobExecutionResult> ExecuteAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Starting long-running process.");

        for (int i = 0; i < 100; i++)
        {
            // Check for cancellation before starting a unit of work
            if (cancellationToken.IsCancellationRequested)
            {
                logger.LogWarning("Job was cancelled. Aborting.");
                return JobExecutionResult.Failure();
            }

            // Pass the token to I/O-bound or cancellable operations
            await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
            logger.LogTrace("Completed step {Step}", i + 1);
        }

        logger.LogInformation("Long-running process finished successfully.");
        return JobExecutionResult.Success();
    }
}
```

### Stable Job Identity

The job name is used as the primary key in the database to track execution history. By default, the system uses the job's class name (e.g., `DataProcessingJob`). This is brittle; if you rename or move the class, its execution history will be lost, and it will be treated as a new job.

The `[JobName]` attribute decouples the job's identity from its implementation details. Once a job is in production, its `[JobName]` should be considered immutable.

The system will throw an exception on startup if it detects two jobs registered with the same name.

## Building From Source

1.  Clone the repository:
    ```shell
    git clone https://github.com/scippo97sensibleproductions/NOBS.JobSystem.git
    ```
2.  Navigate to the solution directory:
    ```shell
    cd NOBS.JobSystem
    ```
3.  Build the solution:
    ```shell
    dotnet build
    ```

## License

This project is licensed under the MIT License. See the [LICENSE](LICENSE) file for details.