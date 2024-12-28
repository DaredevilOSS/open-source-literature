using Extensions;

namespace DataLoader;

public class Config
{
    public string ScrapeDir { get; } = EnvHelper.GetStringEnvOrDefault("SCRAPE_DIR", "/tmp/open-source-literature/scraped");
    
    public string ConnectionString { get; } = EnvHelper.GetStringEnvOrDefault("CONNECTION_STRING", "host=localhost;database=literature");
}