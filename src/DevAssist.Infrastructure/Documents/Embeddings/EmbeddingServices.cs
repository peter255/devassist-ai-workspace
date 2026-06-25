using DevAssist.Application.Interfaces.Documents;
using DevAssist.Infrastructure.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DevAssist.Infrastructure.Documents.Embeddings;

public sealed class PlaceholderEmbeddingService(ILogger<PlaceholderEmbeddingService> logger) : IEmbeddingService
{
    public Task<IReadOnlyList<float[]>> GenerateEmbeddingsAsync(IReadOnlyList<string> texts, CancellationToken cancellationToken)
    {
        // TODO: Azure integration — replace with Azure OpenAI embedding deployment when configured.
        logger.LogWarning("Using placeholder embeddings for {Count} chunks. Configure Azure OpenAI for production vectors.", texts.Count);
        IReadOnlyList<float[]> embeddings = texts.Select(_ => Array.Empty<float>()).ToList();
        return Task.FromResult(embeddings);
    }
}

public sealed class AzureOpenAiEmbeddingService(
    IOptions<AzureOpenAiOptions> options,
    ILogger<AzureOpenAiEmbeddingService> logger) : IEmbeddingService
{
    public async Task<IReadOnlyList<float[]>> GenerateEmbeddingsAsync(IReadOnlyList<string> texts, CancellationToken cancellationToken)
    {
        var settings = options.Value;
        if (string.IsNullOrWhiteSpace(settings.Endpoint) || string.IsNullOrWhiteSpace(settings.ApiKey))
        {
            throw new InvalidOperationException("Azure OpenAI is not configured for embeddings.");
        }

        // TODO: Azure integration — wire Azure.AI.OpenAI embedding client using deployment settings.
        logger.LogWarning("Azure OpenAI embedding client is not fully wired yet.");
        await Task.CompletedTask;
        return texts.Select(_ => Array.Empty<float>()).ToList();
    }
}
