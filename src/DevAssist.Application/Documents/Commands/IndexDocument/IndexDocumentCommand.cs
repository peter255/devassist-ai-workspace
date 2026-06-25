using DevAssist.Contracts.Documents;
using MediatR;

namespace DevAssist.Application.Documents.Commands.IndexDocument;

public sealed record IndexDocumentCommand(Guid DocumentId) : IRequest<IndexDocumentResponse>;
