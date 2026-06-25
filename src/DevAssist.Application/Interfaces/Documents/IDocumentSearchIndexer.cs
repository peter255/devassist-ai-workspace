using DevAssist.Domain.Entities;

namespace DevAssist.Application.Interfaces.Documents;

public sealed record SearchChunkDocument(
    string SearchDocumentKey,
    Guid DocumentId,
    string DocumentName,
    string DocumentType,
    int ChunkOrder,
    string Content,
    float[]? Embedding);

public interface IDocumentSearchIndexer
{
    Task UpsertChunksAsync(IReadOnlyList<SearchChunkDocument> chunks, CancellationToken cancellationToken);
}
