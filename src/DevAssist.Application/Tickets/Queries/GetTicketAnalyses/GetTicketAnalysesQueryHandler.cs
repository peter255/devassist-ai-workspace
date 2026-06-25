using DevAssist.Application.Interfaces.Tickets;
using DevAssist.Application.Tickets.Mappings;
using DevAssist.Contracts.Tickets;
using MediatR;

namespace DevAssist.Application.Tickets.Queries.GetTicketAnalyses;

public sealed class GetTicketAnalysesQueryHandler(ITicketAnalysisRepository repository)
    : IRequestHandler<GetTicketAnalysesQuery, IReadOnlyList<TicketAnalysisListItemDto>>
{
    public async Task<IReadOnlyList<TicketAnalysisListItemDto>> Handle(
        GetTicketAnalysesQuery request,
        CancellationToken cancellationToken)
    {
        var analyses = await repository.GetRecentAsync(request.Limit, cancellationToken);
        return analyses.Select(TicketMapper.ToListItem).ToList();
    }
}
