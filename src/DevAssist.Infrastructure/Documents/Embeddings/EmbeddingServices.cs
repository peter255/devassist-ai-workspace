using Azure;
using Azure.AI.OpenAI;
using DevAssist.Application.Interfaces.Documents;
using DevAssist.Infrastructure.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenAI;
using OpenAI.Embeddings;
using System.ClientModel;

namespace DevAssist.Infrastructure.Documents.Embeddings;

public sealed class PlaceholderEmbeddingService(ILogger<PlaceholderEmbeddingService> logger) : IEmbeddingService
{
    public Task<IReadOnlyList<float[]>> GenerateEmbeddingsAsync(
        IReadOnlyList<string> texts,
        CancellationToken cancellationToken)
    {
        logger.LogWarning(
            "Using placeholder embeddings for {Count} chunks — Azure OpenAI is not configured. " +
            "Documents will be indexed without vectors; only keyword search will be available.",
            texts.Count);

        IReadOnlyList<float[]> embeddings = texts.Select(_ => Array.Empty<float>()).ToList();
        return Task.FromResult(embeddings);
    }
}

/// <summary>
/// Generates dense embedding vectors using Azure OpenAI (or Azure AI Foundry) embedding deployments.
/// Supports both standard Azure OpenAI endpoints and /v1 Foundry-compatible endpoints.
/// Texts are batched to stay within API limits.
/// </summary>
public sealed class AzureOpenAiEmbeddingService(
    IOptions<AzureOpenAiOptions> options,
    ILogger<AzureOpenAiEmbeddingService> logger) : IEmbeddingService
{
    // Azure OpenAI embedding API accepts up to 2048 inputs per request.
    private const int BatchSize = 16;

    public async Task<IReadOnlyList<float[]>> GenerateEmbeddingsAsync(
        IReadOnlyList<string> texts,
        CancellationToken cancellationToken)
    {
        var settings = options.Value;
        if (string.IsNullOrWhiteSpace(settings.Endpoint) || string.IsNullOrWhiteSpace(settings.ApiKey))
        {
            logger.LogWarning(
                "Azure OpenAI endpoint/key not configured. Skipping embeddings — keyword search only.");
            return texts.Select(_ => Array.Empty<float>()).ToList();
        }

        var deploymentName = settings.EffectiveEmbeddingDeployment;
        if (string.IsNullOrWhiteSpace(deploymentName))
        {
            logger.LogWarning(
                "AzureOpenAi:EmbeddingDeploymentName is not set. " +
                "Skipping embeddings — keyword search only. " +
                "Set it to your embedding deployment name (e.g. text-embedding-3-small).");
            return texts.Select(_ => Array.Empty<float>()).ToList();
        }

        logger.LogInformation(
            "Generating embeddings for {Count} texts using deployment '{Deployment}' at '{Endpoint}'.",
            texts.Count, deploymentName, settings.Endpoint);

        try
        {
            var embeddingClient = CreateEmbeddingClient(settings, deploymentName);
            var results = new List<float[]>(texts.Count);

            // Process in batches to respect API concurrency limits.
            for (var i = 0; i < texts.Count; i += BatchSize)
            {
                var batch = texts.Skip(i).Take(BatchSize).ToList();
                var response = await embeddingClient.GenerateEmbeddingsAsync(batch, cancellationToken: cancellationToken);
                foreach (var embedding in response.Value)
                    results.Add(embedding.ToFloats().ToArray());
            }

            logger.LogInformation("Successfully generated {Count} embeddings.", results.Count);
            return results;
        }
        catch (Exception ex)
        {
            // Do NOT let embedding failures cascade into a document indexing failure.
            // The document will still be indexed in SQL and is fully searchable via keyword search.
            // Vector/semantic search will be unavailable until embeddings are configured correctly.
            logger.LogWarning(ex,
                "Azure OpenAI embedding generation failed for deployment '{Deployment}' (endpoint: {Endpoint}). " +
                "Possible causes: model not deployed, wrong deployment name, or endpoint mismatch. " +
                "Falling back to empty embeddings — document will be indexed with keyword search only.",
                deploymentName, settings.Endpoint);
            return texts.Select(_ => Array.Empty<float>()).ToList();
        }
    }

    private static EmbeddingClient CreateEmbeddingClient(AzureOpenAiOptions settings, string deploymentName)
    {
        var endpoint = settings.Endpoint.TrimEnd('/');

        if (endpoint.EndsWith("/v1", StringComparison.OrdinalIgnoreCase))
        {
            var baseUrl = endpoint + "/";
            var clientOptions = new OpenAIClientOptions { Endpoint = new Uri(baseUrl) };
            var client = new OpenAIClient(new ApiKeyCredential(settings.ApiKey), clientOptions);
            return client.GetEmbeddingClient(deploymentName);
        }

        var azureClient = new AzureOpenAIClient(
            new Uri(endpoint + "/"),
            new AzureKeyCredential(settings.ApiKey));

        return azureClient.GetEmbeddingClient(deploymentName);
    }
}
