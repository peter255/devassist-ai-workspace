using DevAssist.Contracts.Documents;
using MediatR;

namespace DevAssist.Application.Documents.Queries.GetDocuments;

public sealed record GetDocumentsQuery : IRequest<IReadOnlyList<DocumentSummaryDto>>;
