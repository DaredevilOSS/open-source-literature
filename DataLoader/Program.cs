using Microsoft.Extensions.Logging;

namespace DataLoader;

internal static class Program
{
    private static readonly ILogger Logger = LoggerFactory.Create(builder => builder.AddConsole()).CreateLogger("DataLoader");

    private static Config Config { get; } = new();

    private static StateDirectory StateDirectory { get; } = new(Config, Logger);
    
    private static Scraper Scraper { get; } = new(Config, Logger, StateDirectory);
    
    private static Indexer Indexer { get;  } = new (Config, Logger, StateDirectory);
    
    public static async Task Main()
    {
        await Scraper.Scrape();
        await Indexer.Index();
    }
}