namespace DevAssist.Application.Interfaces.Copilot;

public sealed record RetrievedChunk(
    Guid DocumentId,
    string DocumentName,
    string DocumentType,
    string ChunkReference,
    string Content,
    double Score);

public interface IDocumentSearchRetriever
{
    Task<IReadOnlyList<RetrievedChunk>> SearchAsync(
        string question,
        int top = 5,
        CancellationToken cancellationToken = default);
}
