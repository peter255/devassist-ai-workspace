using DevAssist.Contracts.Documents;
using MediatR;

namespace DevAssist.Application.Documents.Queries.GetDocumentById;

public sealed record GetDocumentByIdQuery(Guid DocumentId) : IRequest<DocumentDetailsDto>;
