using DevAssist.Application.Interfaces.Documents;
using System.Threading.Channels;

namespace DevAssist.Infrastructure.Documents.BackgroundIndexing;

/// <summary>
/// In-memory, unbounded, FIFO queue backed by System.Threading.Channels.
/// Registered as a singleton so it is shared between the HTTP pipeline (producers)
/// and the BackgroundDocumentIndexingService (consumer).
/// </summary>
public sealed class DocumentIndexingQueue : IDocumentIndexingQueue
{
    private readonly Channel<Guid> _channel = Channel.CreateUnbounded<Guid>(
        new UnboundedChannelOptions { SingleReader = true, AllowSynchronousContinuations = false });

    public void Enqueue(Guid documentId)
        => _channel.Writer.TryWrite(documentId);

    public ValueTask<Guid> DequeueAsync(CancellationToken cancellationToken)
        => _channel.Reader.ReadAsync(cancellationToken);
}
