using DevAssist.Application.Interfaces;
using DevAssist.Application.Interfaces.Documents;

namespace DevAssist.Infrastructure.Documents.Indexing;

public sealed class DocumentIndexingServiceAdapter(IDocumentIndexingOrchestrator orchestrator) : IDocumentIndexingService
{
    public async Task IndexDocumentAsync(Guid documentId, CancellationToken cancellationToken)
    {
        await orchestrator.IndexAsync(documentId, cancellationToken);
    }
}
