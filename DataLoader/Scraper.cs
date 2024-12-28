using DataLoader.Scrapers;
using Microsoft.Extensions.Logging;

namespace DataLoader;

public class Scraper(Config config, ILogger logger, StateDirectory stateDirectory)
{
    private List<IScraper> Scrapers { get; } =
    [
        new GutenbergProject(logger, stateDirectory)
    ];
    
    public async Task Scrape()
    {
        foreach (var s in Scrapers)
            await s.Scrape();
    }
}