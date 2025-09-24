# NOBS.JobSystem

[![NuGet Version](https://img.shields.io/nuget/v/NOBS.JobSystem.svg?style=flat-square)](https://www.nuget.org/packages/NOBS.JobSystem/)
[![NuGet Version](https://img.shields.io/nuget/v/NOBS.JobSystem.UI.svg?style=flat-square)](https://www.nuget.org/packages/NOBS.JobSystem.UI/)
[![NuGet Version](https://img.shields.io/nuget/v/NOBS.JobSystem.Hosting.svg?style=flat-square)](https://www.nuget.org/packages/NOBS.JobSystem.Hosting/)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg?style=flat-square)](https://opensource.org/licenses/MIT)

A lightweight, database-backed, dependency-injection friendly job scheduling system for .NET applications. It provides CRON-based scheduling, job chaining, and a persistent execution history with an optional Blazor-based monitoring UI.

## Key Features

- **CRON Scheduling:** Define recurring jobs using standard CRON expressions.
- **Job Chaining:** Create workflows by specifying continuation jobs on success or failure.
- **Persistent History:** Automatically tracks the last successful run time for each job in a database, ensuring schedules resume correctly after an application restart.
- **DI-First Design:** Jobs are resolved from the service container, giving them access to all registered application services.
- **Error Handling:** Configure specific jobs to run when a preceding job fails or throws an exception.
- **Optional Monitoring UI:** A clean, lightweight Blazor UI to monitor job statuses, last run times, and next scheduled runs.
- **EF Core Migrations:** The database schema for storing job history is managed automatically via Entity Framework Core migrations.

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

### 1. Define a Job

Create a class that implements the `IJob` interface.

```csharp
// src/MyWebApp/Jobs/HelloWorldJob.cs

using NOBS.JobSystem.Abstractions;

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

In your `Program.cs`, add the job system, configure the database connection, and register your jobs.

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
        options.PollingFrequency = TimeSpan.FromSeconds(30); // Optional: Default is 1 minute
    },
    registry =>
    {
        // Run every minute
        registry.AddJob<HelloWorldJob>("* * * * *");
        
        // Example of a chained job
        registry.AddJob<InitialJob>("0 * * * *") // Run hourly
            .OnError<ErrorLoggingJob>();         // Run ErrorLoggingJob if InitialJob fails
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

The system will automatically create and migrate the database on startup to add its `ExecutionHistory` table.

## Advanced Usage

### Job Chaining

Jobs can trigger other jobs upon completion.

-   `JobExecutionResult.Success(Type nextJob)`: Queues a new job immediately if the current job succeeds.
-   `JobExecutionResult.Failure(Type nextJob)`: Queues a new job immediately if the current job returns a failure result.
-   `.OnError<TErrorJob>()`: A declarative way to specify an error-handling job if the configured job throws an unhandled exception.

```csharp
public class DataProcessingJob : IJob
{
    public async Task<JobExecutionResult> ExecuteAsync(CancellationToken cancellationToken)
    {
        bool success = await DoSomeWorkAsync();

        if (success)
        {
            // On success, trigger the notification job
            return JobExecutionResult.Success(typeof(NotificationJob));
        }

        // On failure, trigger the cleanup job
        return JobExecutionResult.Failure(typeof(CleanupJob));
    }
}

// In Program.cs
registry.AddJob<DataProcessingJob>("0 2 * * *") // Run daily at 2 AM
    .OnError<CriticalFailureAlertJob>();       // Run this if DataProcessingJob throws
```

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