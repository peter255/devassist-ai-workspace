using DevAssist.Application.Interfaces.Documents;
using DevAssist.Infrastructure.Documents.Indexing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace DevAssist.Infrastructure.Documents.BackgroundIndexing;

/// <summary>
/// Long-running background service that processes the document indexing queue.
/// Consumes document IDs enqueued by upload handlers and executes the full
/// indexing pipeline (extraction → chunking → embeddings → Azure AI Search / SQL)
/// without blocking the upload HTTP response.
///
/// Each indexing run creates a fresh DI scope so that scoped services like
/// DbContext, repositories, and Azure SDK clients are properly isolated.
/// </summary>
public sealed class BackgroundDocumentIndexingService(
    IDocumentIndexingQueue queue,
    IServiceScopeFactory scopeFactory,
    ILogger<BackgroundDocumentIndexingService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("BackgroundDocumentIndexingService started — waiting for documents.");

        while (!stoppingToken.IsCancellationRequested)
        {
            Guid documentId;
            try
            {
                documentId = await queue.DequeueAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }

            await IndexDocumentAsync(documentId, stoppingToken);
        }

        logger.LogInformation("BackgroundDocumentIndexingService stopped.");
    }

    private async Task IndexDocumentAsync(Guid documentId, CancellationToken stoppingToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var orchestrator = scope.ServiceProvider.GetRequiredService<IDocumentIndexingOrchestrator>();

        logger.LogInformation("Background indexing started for document {DocumentId}.", documentId);
        try
        {
            var result = await orchestrator.IndexAsync(documentId, stoppingToken);
            logger.LogInformation(
                "Background indexing completed for document {DocumentId}: status={Status}, chunks={ChunkCount}.",
                documentId, result.Status, result.ChunkCount);
        }
        catch (OperationCanceledException)
        {
            logger.LogWarning(
                "Background indexing cancelled for document {DocumentId}.", documentId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "Background indexing failed for document {DocumentId}. " +
                "Document status has been set to Failed by the orchestrator.",
                documentId);
        }
    }
}
