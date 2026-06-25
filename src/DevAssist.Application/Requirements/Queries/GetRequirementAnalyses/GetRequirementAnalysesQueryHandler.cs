using DevAssist.Application.Interfaces.Requirements;
using DevAssist.Application.Requirements.Mappings;
using DevAssist.Contracts.Requirements;
using MediatR;

namespace DevAssist.Application.Requirements.Queries.GetRequirementAnalyses;

public sealed class GetRequirementAnalysesQueryHandler(IRequirementAnalysisRepository repository)
    : IRequestHandler<GetRequirementAnalysesQuery, IReadOnlyList<RequirementAnalysisListItemDto>>
{
    public async Task<IReadOnlyList<RequirementAnalysisListItemDto>> Handle(
        GetRequirementAnalysesQuery request,
        CancellationToken cancellationToken)
    {
        var analyses = await repository.GetRecentAsync(request.Limit, cancellationToken);
        return analyses.Select(RequirementMapper.ToListItem).ToList();
    }
}
