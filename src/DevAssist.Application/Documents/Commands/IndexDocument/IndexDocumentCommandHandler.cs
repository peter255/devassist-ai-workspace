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

/// <summary>
/// Explicit re-index endpoint handler. Enqueues the document for background processing
/// and returns immediately with status "Queued". Use this endpoint to re-index a document
/// after updating its content or when the initial background indexing failed.
/// </summary>
public sealed class IndexDocumentCommandHandler(
    IDocumentRepository documentRepository,
    IDocumentIndexingQueue indexingQueue) : IRequestHandler<IndexDocumentCommand, IndexDocumentResponse>
{
    public async Task<IndexDocumentResponse> Handle(IndexDocumentCommand request, CancellationToken cancellationToken)
    {
        var document = await documentRepository.GetByIdAsync(request.DocumentId, cancellationToken)
            ?? throw new KeyNotFoundException($"Document '{request.DocumentId}' was not found.");

        indexingQueue.Enqueue(document.Id);
        return new IndexDocumentResponse(document.Id, "Queued", 0);
    }
}
