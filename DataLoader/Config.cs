using Extensions;

namespace DataLoader;

public class Config
{
    public string ScrapeDir { get; } = EnvHelper.GetStringEnvOrDefault("SCRAPE_DIR", "/tmp/open-source-literature/scraped");

    public int CopyBatchSize { get; } = EnvHelper.GetIntEnvOrDefault("COPY_BATCH_SIZE", 100);
    
    public string ConnectionString { get; } = EnvHelper.GetStringEnvOrDefault("CONNECTION_STRING", "host=localhost;database=literature");
}