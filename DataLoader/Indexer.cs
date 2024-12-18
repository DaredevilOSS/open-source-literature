using Microsoft.Extensions.Logging;
using QueriesGen;

namespace DataLoader;

public class Indexer(Config config, ILogger logger, StateDirectory stateDirectory)
{
    private const int MaxPageSize = 262143;
    
    public async Task Index()
    {
        var queriesSql = new QueriesSql(config.ConnectionString);
        var scrapedTargets = await stateDirectory.GetScrapedTargets();
        var batches = scrapedTargets.Chunk(config.CopyBatchSize).ToList();
 
        await queriesSql.TruncateInterim();
        var maxIdResult = await queriesSql.GetMaxId();
        var currentTextId = maxIdResult?.Max_id ?? 0;
        
        foreach (var batch in batches)
        {
            var copyToInterimArgs = new List<QueriesSql.CopyToInterimArgs>();
            foreach (var scrapeTarget in batch)
            {
                currentTextId++;
                var text = await File.ReadAllTextAsync(scrapeTarget.TextPath);
                var pages = SplitTextToPages(text);
                copyToInterimArgs.AddRange(pages.Select((pageText, i) => 
                    new QueriesSql.CopyToInterimArgs
                    {
                        Text_id = currentTextId,
                        Source = scrapeTarget.Source,
                        Source_updated_at = scrapeTarget.UpdatedAt.ToDateTime(),
                        Author = scrapeTarget.Author,
                        Title = scrapeTarget.Title,
                        Release_date = scrapeTarget.ReleaseDate.ToDateTime(),
                        Page = i + 1,
                        Text = pageText
                    }
                ));
            }
            await queriesSql.CopyToInterim(copyToInterimArgs);
        }
        logger.LogDebug("Copied {count} records to interim table", scrapedTargets.Count);

        await queriesSql.InsertMissingTitles(new QueriesSql.InsertMissingTitlesArgs
        {
            Certainty = (float)0.95
        });
        await queriesSql.InsertMissingPages();
    }

    private static List<string> SplitTextToPages(string text)
    {
        var chunks = new List<string>();
        var startIndex = 0;
        while (startIndex < text.Length)
        {
            var lengthToTake = Math.Min(MaxPageSize, text.Length - startIndex);
            var endIndex = startIndex + lengthToTake;
            if (endIndex >= text.Length) 
            {
                // this is the latest chunk
                chunks.Add(text[startIndex..]);
                break;
            }

            var lastSpace = text.LastIndexOf(' ', endIndex);
            if (lastSpace > startIndex) 
            {
                // ensuring we don't split in the middle of a word.
                chunks.Add(text.Substring(startIndex, lastSpace - startIndex));
                startIndex = lastSpace + 1;
            }
            else
            {
                // No space found - cut at the maxSize limit to avoid infinite loops.
                chunks.Add(text.Substring(startIndex, lengthToTake));
                startIndex += lengthToTake;
            }
        }

        return chunks;
    }
}