using DevAssist.Application.Documents.Mappings;
using DevAssist.Application.Interfaces.Documents;
using DevAssist.Contracts.Documents;
using MediatR;

namespace DevAssist.Application.Documents.Queries.GetDocumentById;

public sealed class GetDocumentByIdQueryHandler(IDocumentRepository documentRepository)
    : IRequestHandler<GetDocumentByIdQuery, DocumentDetailsDto>
{
    public async Task<DocumentDetailsDto> Handle(GetDocumentByIdQuery request, CancellationToken cancellationToken)
    {
        var document = await documentRepository.GetByIdAsync(request.DocumentId, cancellationToken)
            ?? throw new KeyNotFoundException($"Document '{request.DocumentId}' was not found.");

        return DocumentMapper.ToDetails(document);
    }
}
