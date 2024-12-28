using System.Net;
using System.Text.RegularExpressions;
using Extensions;
using Google.Protobuf.WellKnownTypes;
using HtmlAgilityPack;
using Microsoft.Extensions.Logging;

namespace DataLoader.Scrapers;

internal enum ScrapeField
{
    Title,
    Author,
    ReleaseDate,
    Language,
    UpdatedAt
}

internal static class ScrapeTargetExtensions
{
    public static void ParseField(this ScrapeTarget me, ScrapeField type, string value)
    {
        switch (type)
        {
            case ScrapeField.Title:
                me.Title = value;
                break;
            case ScrapeField.Author:
                me.Author = value;
                break;
            case ScrapeField.ReleaseDate:
                var substrEnd = value.IndexOf("[eBook", StringComparison.OrdinalIgnoreCase);
                if (substrEnd > 0)
                {
                    value = value[..substrEnd].Trim();
                    var releaseDate = DateTime.Parse(value).ToUniversalTime();
                    me.ReleaseDate = Timestamp.FromDateTime(releaseDate);
                }
                break;
            case ScrapeField.Language:
                me.Language = value;
                break;
            case ScrapeField.UpdatedAt:
                var updatedAt = DateTime.Parse(value).ToUniversalTime();
                me.UpdatedAt = Timestamp.FromDateTime(updatedAt);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(type), "Unknown field type");
        }
    }
}

public partial class GutenbergProject(ILogger logger, StateDirectory stateDirectory) : IScraper
{
    private const string UrlTemplate = "https://www.gutenberg.org/cache/epub/{idx}/pg{idx}-images.html";

    private int MaxTargets { get; } = EnvHelper.GetIntEnvOrDefault("MAX_GUTENBERG_TARGETS", 2000);

    private static Dictionary<ScrapeField, HashSet<string>> RequiredFields { get; } = new() {
        { ScrapeField.Title, ["title"] },
        { ScrapeField.Author, ["author", "editor"] },
        { ScrapeField.ReleaseDate, ["release date"] },
        { ScrapeField.Language, ["language"] },
        { ScrapeField.UpdatedAt, ["most recently updated"] }
    };
    
    private HttpClient Client { get; } = new();
    
    public async Task Scrape()
    {
        var scrapedTargets = await stateDirectory.GetScrapedTargets();
        var scrapeIdx = 0;
        var cntScraped = 0;
        while (cntScraped <= MaxTargets)
        {
            scrapeIdx++;
            var url = UrlTemplate.Replace("{idx}", scrapeIdx.ToString());
            if (scrapedTargets.Any(t => t.Source == url)) continue;
            
            try
            {
                cntScraped++;
                await ScrapeTargetUrl(url);
            }
            catch (HttpRequestException e)
            {
                if (e.StatusCode == HttpStatusCode.NotFound) continue;
                throw;
            }
            catch (Exception e)
            {
                logger.LogError(e, "Could not scrape URL {url}", url);
            }
        }
    }
    
    private async Task ScrapeTargetUrl(string url)
    {
        var html = await Client.GetStringAsync(url);
        var doc = new HtmlDocument();
        doc.LoadHtml(html);

        var textNode = doc.DocumentNode.SelectSingleNode("//pre") ?? doc.DocumentNode.SelectSingleNode("//body");
        var text = textNode.InnerText;
        var lines = text.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
        
        var scrapeTarget = new ScrapeTarget { Source = url };
        foreach (var line in lines)
        {
            var trimmed = line.Trim().ToLower();
            foreach (var requiredField in RequiredFields)
            {
                if (!requiredField.Value.Any(lookup => trimmed.StartsWith($"{lookup}:"))) continue;

                var separatorIdx = trimmed.IndexOf(':') + 1;
                var val = trimmed[separatorIdx..].Trim();
                scrapeTarget.ParseField(requiredField.Key, val);
            }
        }
        
        var (startIndex, endIndex) = FindTextStartAndEnd(lines);
        if (startIndex < 0 || endIndex <= startIndex || endIndex > lines.Length) return;
        
        var textLines = lines.Skip(startIndex).Take(endIndex - startIndex);
        var fullText = string.Join("\n", textLines);
        await stateDirectory.Persist(scrapeTarget, fullText);
    }

    private static (int , int) FindTextStartAndEnd(string[] lines)
    {
        var startRegex = StartTextRegex();
        var endRegex = EndTextRegex();
        
        var startIndex = -1;
        var endIndex = -1;

        for (var i = 0; i < lines.Length; i++)
        {
            if (startIndex == -1 && startRegex.IsMatch(lines[i]))
            {
                startIndex = i + 1;
            }
            else if (endRegex.IsMatch(lines[i]))
            {
                endIndex = i;
                break;
            }
        }

        return (startIndex, endIndex);
    }

    [GeneratedRegex(@"\*\*\* START OF THE PROJECT GUTENBERG EBOOK.*\*\*\*", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex StartTextRegex();
    
    [GeneratedRegex(@"\*\*\* END OF THE PROJECT GUTENBERG EBOOK.*\*\*\*", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex EndTextRegex();
}