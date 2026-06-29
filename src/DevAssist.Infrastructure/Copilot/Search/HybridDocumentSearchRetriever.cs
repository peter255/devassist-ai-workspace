using DevAssist.Application.Interfaces.Copilot;
using Microsoft.Extensions.Logging;

namespace DevAssist.Infrastructure.Copilot.Search;

/// <summary>
/// Queries Azure AI Search first; falls back to SQL keyword search when Azure Search
/// returns no results (e.g. index not yet populated) or is unavailable.
/// </summary>
public sealed class HybridDocumentSearchRetriever(
    AzureSearchDocumentRetriever azureRetriever,
    SqlDocumentSearchRetriever sqlRetriever,
    ILogger<HybridDocumentSearchRetriever> logger) : IDocumentSearchRetriever
{
    public async Task<IReadOnlyList<RetrievedChunk>> SearchAsync(
        string question,
        int top = 5,
        CancellationToken cancellationToken = default)
    {
        var azureResults = await azureRetriever.SearchAsync(question, top, cancellationToken);
        if (azureResults.Count > 0)
            return azureResults;

        logger.LogInformation(
            "Azure AI Search returned 0 results; falling back to SQL keyword retrieval.");
        return await sqlRetriever.SearchAsync(question, top, cancellationToken);
    }
}
