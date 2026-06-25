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
        {
            throw new InvalidOperationException("Azure Search is not configured.");
        }

        // TODO: Azure integration — wire Azure.Search.Documents SearchClient with vector/hybrid query.
        logger.LogWarning(
            "Azure Search retrieval is scaffolded for index {IndexName}. Falling back to empty results until SDK query is wired.",
            settings.IndexName);

        await Task.CompletedTask;
        return [];
    }
}
