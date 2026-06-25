using DevAssist.Application.Interfaces.Documents;
using DevAssist.Contracts.Documents;
using FluentValidation;
using MediatR;

namespace DevAssist.Application.Documents.Commands.IndexDocument;

public sealed class IndexDocumentCommandValidator : AbstractValidator<IndexDocumentCommand>
{
    public IndexDocumentCommandValidator()
    {
        RuleFor(x => x.DocumentId).NotEmpty();
    }
}

public sealed class IndexDocumentCommandHandler(
    IDocumentRepository documentRepository,
    IDocumentIndexingOrchestrator indexingOrchestrator) : IRequestHandler<IndexDocumentCommand, IndexDocumentResponse>
{
    public async Task<IndexDocumentResponse> Handle(IndexDocumentCommand request, CancellationToken cancellationToken)
    {
        var document = await documentRepository.GetByIdAsync(request.DocumentId, cancellationToken)
            ?? throw new KeyNotFoundException($"Document '{request.DocumentId}' was not found.");

        var result = await indexingOrchestrator.IndexAsync(document.Id, cancellationToken);
        return new IndexDocumentResponse(document.Id, result.Status, result.ChunkCount);
    }
}
