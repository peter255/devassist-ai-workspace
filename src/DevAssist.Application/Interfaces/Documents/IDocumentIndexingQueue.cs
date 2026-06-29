namespace DevAssist.Application.Interfaces.Documents;

/// <summary>
/// Async queue that decouples document upload from the indexing pipeline.
/// Upload handlers enqueue a document ID immediately after saving metadata.
/// The BackgroundDocumentIndexingService dequeues and runs the full indexing pipeline
/// without blocking the HTTP request.
/// </summary>
public interface IDocumentIndexingQueue
{
    /// <summary>Enqueues a document for background indexing. Thread-safe.</summary>
    void Enqueue(Guid documentId);

    /// <summary>
    /// Asynchronously dequeues the next document ID.
    /// Blocks until an item is available or cancellation is requested.
    /// </summary>
    ValueTask<Guid> DequeueAsync(CancellationToken cancellationToken);
}
