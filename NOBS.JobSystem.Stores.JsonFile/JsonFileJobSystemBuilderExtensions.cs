using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NOBS.JobSystem.Abstractions;
using NOBS.JobSystem.Execution;
using NOBS.JobSystem.Stores.JsonFile.Persistence;

namespace NOBS.JobSystem.Stores.JsonFile;

public static class JsonFileJobSystemBuilderExtensions
{
    public static IJobSystemBuilder UseJsonFile(
        this IJobSystemBuilder builder,
        Action<JsonFileOptions> configure)
    {
        builder.Services.Configure(configure);
        
        builder.Services.TryAddSingleton(JobHistoryJsonContext.Default.DictionaryStringDateTimeOffset);
        builder.Services.TryAddSingleton<IJobHistoryStore, JsonFileJobHistoryStore>();
        
        builder.Services.AddOptions<JobSystemOptions>()
            .Configure(options =>
            {
                var fileOptions = new JsonFileOptions();
                configure(fileOptions);
                options.PollingFrequency = fileOptions.PollingFrequency;
            });
        
        return builder;
    }
}