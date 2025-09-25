using System.ComponentModel.DataAnnotations;

namespace NOBS.JobSystem.Stores.JsonFile;

public sealed class JsonFileOptions
{
    [Required(AllowEmptyStrings = false)]
    public string FilePath { get; set; } = "job_history.json";
    
    public TimeSpan PollingFrequency { get; set; } = TimeSpan.FromMinutes(1);
}