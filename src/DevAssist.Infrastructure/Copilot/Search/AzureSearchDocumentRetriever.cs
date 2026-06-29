using Azure;
using Azure.Search.Documents;
using Azure.Search.Documents.Models;
using DevAssist.Application.Interfaces.Copilot;
using DevAssist.Application.Interfaces.Documents;
using DevAssist.Infrastructure.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DevAssist.Infrastructure.Copilot.Search;

/// <summary>
/// Retrieves relevant document chunks from Azure AI Search using a tiered search strategy:
///
/// 1. Vector search — when embedding vectors are present in the index, performs a KNN
///    similarity query against the question's embedding for semantic relevance.
/// 2. Hybrid search — combines BM25 full-text and vector KNN results, merging scores with RRF.
/// 3. Semantic re-ranking — when a semantic configuration name is provided, applies the
///    Azure AI Search semantic ranker on top of hybrid results.
///
/// Gracefully degrades to keyword-only search when vectors are not available or the
/// embedding service returns an empty vector.
/// </summary>
public sealed class AzureSearchDocumentRetriever(
    IOptions<AzureSearchOptions> options,
    IOptions<AzureOpenAiOptions> openAiOptions,
    IEmbeddingService embeddingService,
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

            var searchOptions = BuildSearchOptions(settings, top);

            // Attempt to generate a query embedding for vector/hybrid search.
            float[]? queryVector = null;
            try
            {
                var oas = openAiOptions.Value;
                if (!string.IsNullOrWhiteSpace(oas.Endpoint) && !string.IsNullOrWhiteSpace(oas.ApiKey))
                {
                    var embeddings = await embeddingService.GenerateEmbeddingsAsync(
                        [question], cancellationToken);
                    queryVector = embeddings.Count > 0 && embeddings[0].Length > 0
                        ? embeddings[0]
                        : null;
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex,
                    "Query embedding generation failed; falling back to keyword-only search.");
            }

            if (queryVector is { Length: > 0 })
            {
                // Hybrid: text + vector KNN via RRF fusion.
                searchOptions.VectorSearch = new VectorSearchOptions
                {
                    Queries =
                    {
                        new VectorizedQuery(queryVector)
                        {
                            KNearestNeighborsCount = top,
                            Fields = { "contentVector" }
                        }
                    }
                };

                logger.LogDebug(
                    "Executing hybrid (text + vector) search for question with {Dims}-dim vector.",
                    queryVector.Length);
            }
            else
            {
                logger.LogDebug("No query vector available; executing keyword-only search.");
            }

            var response = await client.SearchAsync<SearchDocument>(question, searchOptions, cancellationToken);

            var chunks = new List<RetrievedChunk>();
            await foreach (var result in response.Value.GetResultsAsync().WithCancellation(cancellationToken))
            {
                var doc = result.Document;
                var score = result.SemanticSearch?.RerankerScore ?? result.Score ?? 0;
                chunks.Add(new RetrievedChunk(
                    DocumentId: GetGuid(doc, "documentId"),
                    DocumentName: GetString(doc, "documentName"),
                    DocumentType: GetString(doc, "documentType"),
                    ChunkReference: GetString(doc, "id"),
                    Content: GetString(doc, "content"),
                    Score: score));
            }

            logger.LogInformation(
                "Azure AI Search returned {Count} chunks (hybrid={IsHybrid}, semantic={IsSemantic}).",
                chunks.Count,
                queryVector is { Length: > 0 },
                !string.IsNullOrWhiteSpace(settings.SemanticConfigurationName));

            return chunks;
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            logger.LogWarning(
                "Azure AI Search index '{IndexName}' does not exist — upload and index documents first.",
                settings.IndexName);
            return [];
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Azure AI Search retrieval failed; returning empty results.");
            return [];
        }
    }

    private static SearchOptions BuildSearchOptions(AzureSearchOptions settings, int top)
    {
        var useSemanticSearch = !string.IsNullOrWhiteSpace(settings.SemanticConfigurationName);

        var opts = new SearchOptions
        {
            Size = top,
            QueryType = useSemanticSearch ? SearchQueryType.Semantic : SearchQueryType.Simple,
            SearchMode = SearchMode.Any,
        };

        opts.Select.Add("id");
        opts.Select.Add("documentId");
        opts.Select.Add("documentName");
        opts.Select.Add("documentType");
        opts.Select.Add("content");

        if (useSemanticSearch)
        {
            opts.SemanticSearch = new SemanticSearchOptions
            {
                SemanticConfigurationName = settings.SemanticConfigurationName,
                QueryCaption = new QueryCaption(QueryCaptionType.Extractive)
                    { HighlightEnabled = true },
                QueryAnswer = new QueryAnswer(QueryAnswerType.Extractive)
                    { Count = 3 }
            };
        }

        return opts;
    }

    private static string GetString(SearchDocument doc, string key)
        => doc.TryGetValue(key, out var val) ? val?.ToString() ?? string.Empty : string.Empty;

    private static Guid GetGuid(SearchDocument doc, string key)
        => Guid.TryParse(GetString(doc, key), out var g) ? g : Guid.Empty;
}
