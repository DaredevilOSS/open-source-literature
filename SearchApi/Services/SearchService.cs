using Grpc.Core;
using QueriesGen;
using Search;

namespace SearchApi.Services;

public class SearchService(ILogger<SearchService> logger) : Searcher.SearcherBase
{
    private static Config Config { get; } = new();
    
    private static QueriesSql QueriesSql { get;  } = new(Config.ConnectionString);

    private const string PgHeadlineOptions = "MaxFragments=10, MaxWords=7, MinWords=3, StartSel=<<, StopSel=>>";
    
    public override async Task<SearchResponse> Search(SearchRequest request, ServerCallContext context)
    {
        var results = string.IsNullOrEmpty(request.Author)
            ? await SearchAllAuthors(request.Query)
            : await SearchSpecificAuthors(request.Query, request.Author);
        logger.LogInformation("Found {cnt} matching documents", results.Count);
        return new SearchResponse { Results = { results } };
    }

    private static async Task<List<SearchResult>> SearchAllAuthors(string query)
    {
        var queryArgs = new QueriesSql.SearchInTextsArgs
        {
            Query = query,
            Limit = Config.ResultsLimit,
            Options = PgHeadlineOptions,
        };
        var results = await QueriesSql.SearchInTexts(queryArgs);
        var i = 0;
        return results.Select(r => new SearchResult
        {
            Id = ++i,
            TextId = r.Text_id,
            Author = r.Author,
            Title = r.Title,
            Source = r.Source,
            Matches = r.Matches
        }).ToList();
    }
    
    private static Task<List<SearchResult>> SearchSpecificAuthors(string query, string author)
    {
        return SearchAllAuthors(query); // TODO fix
    }
}