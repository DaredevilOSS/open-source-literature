using Extensions;

namespace SearchApi;

public class Config
{
    public string ConnectionString { get; } = EnvHelper.GetStringEnvOrDefault("CONNECTION_STRING", "host=localhost;database=literature");

    public int ResultsLimit { get; } = EnvHelper.GetIntEnvOrDefault("RESULTS_LIMIT", 20);
}