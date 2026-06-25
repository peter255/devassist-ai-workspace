using DevAssist.Domain.Entities;

namespace DevAssist.Application.Interfaces.Documents;

public interface IDocumentRepository
{
    Task<Document?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<IReadOnlyList<Document>> GetAllAsync(CancellationToken cancellationToken);
    Task AddAsync(Document document, CancellationToken cancellationToken);
    Task ReplaceChunksAsync(Guid documentId, IReadOnlyList<DocumentChunk> chunks, CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
