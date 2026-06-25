using DevAssist.Application.Interfaces.Documents;
using DevAssist.Domain.Entities;
using DevAssist.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DevAssist.Infrastructure.Documents;

public sealed class DocumentRepository(DevAssistDbContext dbContext) : IDocumentRepository
{
    public Task<Document?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        dbContext.Documents
            .Include(x => x.Chunks)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Document>> GetAllAsync(CancellationToken cancellationToken) =>
        await dbContext.Documents
            .AsNoTracking()
            .OrderByDescending(x => x.UploadedAt)
            .ToListAsync(cancellationToken);

    public async Task AddAsync(Document document, CancellationToken cancellationToken) =>
        await dbContext.Documents.AddAsync(document, cancellationToken);

    public async Task ReplaceChunksAsync(Guid documentId, IReadOnlyList<DocumentChunk> chunks, CancellationToken cancellationToken)
    {
        var existing = await dbContext.DocumentChunks
            .Where(x => x.DocumentId == documentId)
            .ToListAsync(cancellationToken);

        dbContext.DocumentChunks.RemoveRange(existing);
        await dbContext.DocumentChunks.AddRangeAsync(chunks, cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken) =>
        dbContext.SaveChangesAsync(cancellationToken);
}
