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
        logger.LogInformation(
            "No-op search indexer: {Count} chunks stored in SQL only (Azure Search not configured).",
            chunks.Count);
        return Task.CompletedTask;
    }
}

/// <summary>
/// Upserts document chunks into an Azure AI Search index.
/// Supports:
///   • Full-text (BM25) search via the <c>content</c> searchable field
///   • Vector similarity search via the <c>contentVector</c> field (when embeddings are provided)
///   • Semantic ranking via a named semantic configuration (when configured)
///
/// Index creation is idempotent — the index is created on first use if it does not exist.
/// Failures never propagate to callers; documents remain searchable via SQL fallback.
/// </summary>
public sealed class AzureSearchDocumentIndexer(
    IOptions<AzureSearchOptions> options,
    ILogger<AzureSearchDocumentIndexer> logger) : IDocumentSearchIndexer
{
    private const string VectorAlgorithmName = "hnsw-config";
    private const string VectorProfileName = "content-vector-profile";
    private const string SemanticConfigName = "devassist-semantic";

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

            await EnsureIndexExistsAsync(endpoint, credential, settings, cancellationToken);

            var searchClient = new SearchClient(endpoint, settings.IndexName, credential);

            var hasVectors = chunks.Any(c => c.Embedding is { Length: > 0 });

            var documents = chunks.Select(c =>
            {
                var doc = new SearchDocument();
                doc["id"] = c.SearchDocumentKey;
                doc["documentId"] = c.DocumentId.ToString();
                doc["documentName"] = c.DocumentName;
                doc["documentType"] = c.DocumentType;
                doc["chunkOrder"] = c.ChunkOrder;
                doc["content"] = c.Content;

                if (c.Embedding is { Length: > 0 })
                    doc["contentVector"] = c.Embedding;

                return doc;
            }).ToList();

            var batch = IndexDocumentsBatch.Upload(documents);
            var result = await searchClient.IndexDocumentsAsync(batch, cancellationToken: cancellationToken);

            var failed = result.Value.Results.Count(r => !r.Succeeded);
            if (failed > 0)
                logger.LogWarning("{Failed} of {Total} chunks failed to upsert in Azure Search.", failed, chunks.Count);
            else
                logger.LogInformation(
                    "Upserted {Count} chunks to Azure Search index '{IndexName}' (vectors: {HasVectors}).",
                    chunks.Count, settings.IndexName, hasVectors);
        }
        catch (Exception ex)
        {
            // Non-fatal — SQL chunks are the source of truth; Azure Search is an enhancement.
            logger.LogWarning(ex,
                "Azure Search indexing failed for {Count} chunks; document remains searchable via SQL retrieval.",
                chunks.Count);
        }
    }

    private async Task EnsureIndexExistsAsync(
        Uri endpoint,
        AzureKeyCredential credential,
        AzureSearchOptions settings,
        CancellationToken cancellationToken)
    {
        var indexClient = new SearchIndexClient(endpoint, credential);
        try
        {
            await indexClient.GetIndexAsync(settings.IndexName, cancellationToken);
            return;
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            logger.LogInformation("Azure Search index '{IndexName}' not found — creating it.", settings.IndexName);
        }

        var dimensions = settings.VectorDimensions > 0 ? settings.VectorDimensions : 1536;
        var useSemanticConfig = !string.IsNullOrWhiteSpace(settings.SemanticConfigurationName);

        var index = new SearchIndex(settings.IndexName)
        {
            Fields =
            {
                new SimpleField("id", SearchFieldDataType.String)
                    { IsKey = true, IsFilterable = true },
                new SimpleField("documentId", SearchFieldDataType.String)
                    { IsFilterable = true },
                new SearchableField("documentName")
                    { IsFilterable = true, IsSortable = true },
                new SimpleField("documentType", SearchFieldDataType.String)
                    { IsFilterable = true },
                new SimpleField("chunkOrder", SearchFieldDataType.Int32),
                new SearchableField("content"),
                new VectorSearchField("contentVector", dimensions, VectorProfileName),
            },
            VectorSearch = new VectorSearch
            {
                Algorithms =
                {
                    new HnswAlgorithmConfiguration(VectorAlgorithmName)
                    {
                        Parameters = new HnswParameters { Metric = VectorSearchAlgorithmMetric.Cosine }
                    }
                },
                Profiles =
                {
                    new VectorSearchProfile(VectorProfileName, VectorAlgorithmName)
                }
            }
        };

        // Add semantic configuration when a semantic ranker plan is available.
        if (useSemanticConfig)
        {
            var effectiveName = settings.SemanticConfigurationName == "default"
                ? SemanticConfigName
                : settings.SemanticConfigurationName;

            index.SemanticSearch = new SemanticSearch
            {
                Configurations =
                {
                    new SemanticConfiguration(effectiveName, new SemanticPrioritizedFields
                    {
                        ContentFields = { new SemanticField("content") },
                        KeywordsFields = { new SemanticField("documentName"), new SemanticField("documentType") }
                    })
                }
            };

            logger.LogInformation(
                "Semantic search configuration '{Config}' added to index '{IndexName}'.",
                effectiveName, settings.IndexName);
        }

        await indexClient.CreateOrUpdateIndexAsync(index, cancellationToken: cancellationToken);
        logger.LogInformation(
            "Created Azure Search index '{IndexName}' with vector dimensions={Dims}, semantic={Semantic}.",
            settings.IndexName, dimensions, useSemanticConfig);
    }
}
