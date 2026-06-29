using Azure;
using Azure.Search.Documents;
using Azure.Search.Documents.Models;
using DevAssist.Application.Interfaces.Copilot;
using DevAssist.Infrastructure.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DevAssist.Infrastructure.Copilot.Search;

public sealed class AzureSearchDocumentRetriever(
    IOptions<AzureSearchOptions> options,
    ILogger<AzureSearchDocumentRetriever> logger) : IDocumentSearchRetriever
{
    public async Task<IReadOnlyList<RetrievedChunk>> SearchAsync(
        string question,
        int top = 5,
        CancellationToken cancellationToken = default)
    {
        var settings = options.Value;
        if (string.IsNullOrWhiteSpace(settings.Endpoint) || string.IsNullOrWhiteSpace(settings.ApiKey))
            throw new InvalidOperationException("Azure Search is not configured.");

        try
        {
            var client = new SearchClient(
                new Uri(settings.Endpoint),
                settings.IndexName,
                new AzureKeyCredential(settings.ApiKey));

            var searchOptions = new SearchOptions
            {
                Size = top,
                QueryType = SearchQueryType.Simple,
                SearchMode = SearchMode.Any,
            };
            searchOptions.Select.Add("id");
            searchOptions.Select.Add("documentId");
            searchOptions.Select.Add("documentName");
            searchOptions.Select.Add("documentType");
            searchOptions.Select.Add("content");

            var response = await client.SearchAsync<SearchDocument>(question, searchOptions, cancellationToken);

            var chunks = new List<RetrievedChunk>();
            await foreach (var result in response.Value.GetResultsAsync().WithCancellation(cancellationToken))
            {
                var doc = result.Document;
                chunks.Add(new RetrievedChunk(
                    DocumentId: GetGuid(doc, "documentId"),
                    DocumentName: GetString(doc, "documentName"),
                    DocumentType: GetString(doc, "documentType"),
                    ChunkReference: GetString(doc, "id"),
                    Content: GetString(doc, "content"),
                    Score: result.Score ?? 0));
            }

            logger.LogInformation("Azure AI Search returned {Count} chunks for question.", chunks.Count);
            return chunks;
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            logger.LogWarning(
                "Azure AI Search index '{IndexName}' does not exist. Upload and index documents first.",
                settings.IndexName);
            return [];
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Azure AI Search retrieval failed; returning empty results.");
            return [];
        }
    }

    private static string GetString(SearchDocument doc, string key)
        => doc.TryGetValue(key, out var val) ? val?.ToString() ?? string.Empty : string.Empty;

    private static Guid GetGuid(SearchDocument doc, string key)
        => Guid.TryParse(GetString(doc, key), out var g) ? g : Guid.Empty;
}
