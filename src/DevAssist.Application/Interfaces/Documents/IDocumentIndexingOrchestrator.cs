namespace DevAssist.Application.Interfaces.Documents;

public sealed record DocumentIndexingResult(string Status, int ChunkCount);

public interface IDocumentIndexingOrchestrator
{
    Task<DocumentIndexingResult> IndexAsync(Guid documentId, CancellationToken cancellationToken);
}
