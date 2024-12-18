using Google.Protobuf;
using Microsoft.Extensions.Logging;

namespace DataLoader;

public class StateDirectory
{
    private readonly Config _config;
    
    private readonly ILogger _logger;

    public StateDirectory(Config config, ILogger logger)
    {
        _config = config;
        _logger = logger;
        
        var actualPath = Path.GetFullPath(config.ScrapeDir);
        Directory.CreateDirectory(actualPath);
    }

    public async Task Persist(ScrapeTarget scrapeTarget, string text)
    {
        scrapeTarget.TextPath = await PersistText(scrapeTarget.Title, text);
        await PersistMetadata(scrapeTarget);
    }

    private async Task<string> PersistText(string title, string? text)
    {
        var fileName = $"{SanitizeTextFileName(title)}.txt";
        var filePath = Path.Combine(_config.ScrapeDir, fileName);

        try
        {
            await File.WriteAllTextAsync(filePath, text);
            return filePath;
        }
        catch (Exception ex)
        {
            throw new Exception($"Error saving '{fileName}': {ex.Message}");
        }
    }

    private async Task PersistMetadata(ScrapeTarget scrapeTarget)
    {
        var fileName = $"{GenerateMessageFilename(scrapeTarget.Title)}.message";
        var filePath = Path.Combine(_config.ScrapeDir, fileName);
        await using var output = File.Create(filePath);
        scrapeTarget.WriteTo(output);
    }

    private static string SanitizeTextFileName(string fileName)
    {
        return Path
            .GetInvalidFileNameChars()
            .Aggregate(fileName, (current, c) => current.Replace(c, '_'));
    }

    private static string GenerateMessageFilename(string fileName)
    {
        var fullHash = fileName.GetHashCode();
        var shortHash = unchecked((ushort)fullHash);
        var epochMillis = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        return $"{epochMillis}-{shortHash}";
    }
    
    public async Task<List<ScrapeTarget>> GetScrapedTargets()
    {
        var metadataFiles = Directory
            .EnumerateFiles(_config.ScrapeDir, "*.message", SearchOption.TopDirectoryOnly);
        var result = await Task.WhenAll(metadataFiles.Select(ReadMetadataMessage));
        return result.ToList();
    }
    
    private static async Task<ScrapeTarget> ReadMetadataMessage(string filePath)
    {
        await using var input = File.OpenRead(filePath);
        return ScrapeTarget.Parser.ParseFrom(input);
    }
}