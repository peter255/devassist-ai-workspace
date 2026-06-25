using DevAssist.Application.Documents.Mappings;
using DevAssist.Application.Interfaces.Documents;
using DevAssist.Contracts.Documents;
using MediatR;

namespace DevAssist.Application.Documents.Queries.GetDocuments;

public sealed class GetDocumentsQueryHandler(IDocumentRepository documentRepository)
    : IRequestHandler<GetDocumentsQuery, IReadOnlyList<DocumentSummaryDto>>
{
    public async Task<IReadOnlyList<DocumentSummaryDto>> Handle(GetDocumentsQuery request, CancellationToken cancellationToken)
    {
        var documents = await documentRepository.GetAllAsync(cancellationToken);
        return documents.Select(DocumentMapper.ToSummary).ToList();
    }
}
