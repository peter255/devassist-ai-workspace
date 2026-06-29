using Azure;
using Azure.Search.Documents;
using Azure.Search.Documents.Indexes;
using Azure.Search.Documents.Indexes.Models;
using Azure.Search.Documents.Models;
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
    // Azure Search indexing is an optional enhancement on top of SQL chunk storage.
    // Failures here must never propagate — the document is already chunked in SQL and
    // will be retrievable via SqlDocumentSearchRetriever / HybridDocumentSearchRetriever.
    public async Task UpsertChunksAsync(IReadOnlyList<SearchChunkDocument> chunks, CancellationToken cancellationToken)
    {
        var settings = options.Value;
        if (string.IsNullOrWhiteSpace(settings.Endpoint) || string.IsNullOrWhiteSpace(settings.ApiKey))
        {
            logger.LogWarning("Azure Search is not configured; skipping Azure Search indexing.");
            return;
        }

        try
        {
            var credential = new AzureKeyCredential(settings.ApiKey);
            var endpoint = new Uri(settings.Endpoint);

            await EnsureIndexExistsAsync(endpoint, credential, settings.IndexName, cancellationToken);

            var searchClient = new SearchClient(endpoint, settings.IndexName, credential);

            var documents = chunks.Select(c =>
            {
                var doc = new SearchDocument();
                doc["id"] = c.SearchDocumentKey;
                doc["documentId"] = c.DocumentId.ToString();
                doc["documentName"] = c.DocumentName;
                doc["documentType"] = c.DocumentType;
                doc["chunkOrder"] = c.ChunkOrder;
                doc["content"] = c.Content;
                return doc;
            }).ToList();

            var batch = IndexDocumentsBatch.Upload(documents);
            var result = await searchClient.IndexDocumentsAsync(batch, cancellationToken: cancellationToken);

            var failed = result.Value.Results.Count(r => !r.Succeeded);
            if (failed > 0)
                logger.LogWarning("{Failed} of {Total} chunks failed to upsert in Azure Search.", failed, chunks.Count);
            else
                logger.LogInformation("Upserted {Count} chunks to Azure Search index '{IndexName}'.", chunks.Count, settings.IndexName);
        }
        catch (Exception ex)
        {
            // Log and continue — document chunks are already stored in SQL.
            logger.LogWarning(ex,
                "Azure Search indexing failed for {Count} chunks; document will still be searchable via SQL retrieval.",
                chunks.Count);
        }
    }

    private async Task EnsureIndexExistsAsync(
        Uri endpoint,
        AzureKeyCredential credential,
        string indexName,
        CancellationToken cancellationToken)
    {
        var indexClient = new SearchIndexClient(endpoint, credential);
        try
        {
            await indexClient.GetIndexAsync(indexName, cancellationToken);
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            logger.LogInformation("Creating Azure Search index '{IndexName}'.", indexName);
            var index = new SearchIndex(indexName)
            {
                Fields =
                {
                    new SimpleField("id", SearchFieldDataType.String) { IsKey = true, IsFilterable = true },
                    new SimpleField("documentId", SearchFieldDataType.String) { IsFilterable = true },
                    new SearchableField("documentName") { IsFilterable = true },
                    new SimpleField("documentType", SearchFieldDataType.String) { IsFilterable = true },
                    new SimpleField("chunkOrder", SearchFieldDataType.Int32),
                    new SearchableField("content"),
                }
            };
            await indexClient.CreateIndexAsync(index, cancellationToken);
        }
    }
}
