using DevAssist.Contracts.Tickets;
using MediatR;

namespace DevAssist.Application.Tickets.Queries.GetTicketAnalyses;

public sealed record GetTicketAnalysesQuery(int Limit = 20) : IRequest<IReadOnlyList<TicketAnalysisListItemDto>>;
