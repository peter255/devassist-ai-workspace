using DevAssist.Application.Interfaces.Documents;
using DevAssist.Infrastructure.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DevAssist.Infrastructure.Documents.Search;

public sealed class NoOpDocumentSearchIndexer(ILogger<NoOpDocumentSearchIndexer> logger) : IDocumentSearchIndexer
{
    public Task UpsertChunksAsync(IReadOnlyList<SearchChunkDocument> chunks, CancellationToken cancellationToken)
    {
        logger.LogInformation("No-op search indexer stored {Count} chunks locally only (Azure Search not configured).", chunks.Count);
        return Task.CompletedTask;
    }
}

public sealed class AzureSearchDocumentIndexer(
    IOptions<AzureSearchOptions> options,
    ILogger<AzureSearchDocumentIndexer> logger) : IDocumentSearchIndexer
{
    public async Task UpsertChunksAsync(IReadOnlyList<SearchChunkDocument> chunks, CancellationToken cancellationToken)
    {
        var settings = options.Value;
        if (string.IsNullOrWhiteSpace(settings.Endpoint) || string.IsNullOrWhiteSpace(settings.ApiKey))
        {
            throw new InvalidOperationException("Azure Search is not configured.");
        }

        // TODO: Azure integration — upsert chunk documents via Azure.Search.Documents SearchClient.
        logger.LogWarning(
            "Azure Search upsert is scaffolded for {Count} chunks into index {IndexName}. SDK wiring pending.",
            chunks.Count,
            settings.IndexName);

        await Task.CompletedTask;
    }
}
