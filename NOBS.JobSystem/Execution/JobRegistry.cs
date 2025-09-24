namespace NOBS.JobSystem.Execution;

public sealed class JobRegistry
{
    public List<JobConfiguration> JobConfigurations { get; } = [];

    public JobConfiguration AddJob<TJob>(string cronExpression) where TJob : Abstractions.IJob
    {
        var config = new JobConfiguration(typeof(TJob), cronExpression);
        JobConfigurations.Add(config);
        return config;
    }
}