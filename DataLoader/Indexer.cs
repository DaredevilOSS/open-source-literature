using Microsoft.Extensions.Logging;
using QueriesGen;

namespace DataLoader;

public class Indexer(Config config, ILogger logger, StateDirectory stateDirectory)
{
    private const int MaxPageSize = 262143;

    private const int CopyBatchSize = 10;
    
    public async Task Index()
    {
        var queriesSql = new QueriesSql(config.ConnectionString);
        var scrapedTargets = await stateDirectory.GetScrapedTargets();
 
        var maxIdResult = await queriesSql.GetMaxId();
        var currentTextId = maxIdResult?.Max_id + 1 ?? 1;
        
        var batchArgs = new List<QueriesSql.CopyToInterimArgs>();
        for (var i = 0; i < scrapedTargets.Count; i++)
        {
            var scrapeTarget = scrapedTargets[i];
            var sourceUpdatedAt = scrapeTarget.UpdatedAt?.ToDateTime();
            var releaseDate = scrapeTarget.ReleaseDate.ToDateTime();
            var text = await File.ReadAllTextAsync(scrapeTarget.TextPath);
            var pages = SplitTextToPages(text);

            // ReSharper disable once LoopCanBeConvertedToQuery
            for (var j = 0; j < pages.Count; j++)
                batchArgs.Add(new QueriesSql.CopyToInterimArgs
                {
                    Text_id = currentTextId,
                    Source = scrapeTarget.Source,
                    Source_updated_at = sourceUpdatedAt,
                    Author = scrapeTarget.Author,
                    Title = scrapeTarget.Title,
                    Release_date = releaseDate,
                    Page = j + 1,
                    Text = pages[j]
                });
            currentTextId++;

            if (batchArgs.Count < CopyBatchSize && i < scrapedTargets.Count - 1) continue;
            await InsertTextsBatch(queriesSql, batchArgs);
            batchArgs = [];
        }
    }

    private static async Task InsertTextsBatch(QueriesSql queriesSql, List<QueriesSql.CopyToInterimArgs> batchArgs)
    {
        await queriesSql.TruncateInterim();
        await queriesSql.CopyToInterim(batchArgs);
        await queriesSql.InsertTitles();
        await queriesSql.InsertPages();
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