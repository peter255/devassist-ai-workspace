using DevAssist.Application.Interfaces.Requirements;
using DevAssist.Application.Requirements.Mappings;
using DevAssist.Contracts.Requirements;
using MediatR;

namespace DevAssist.Application.Requirements.Queries.GetRequirementAnalysisById;

public sealed class GetRequirementAnalysisByIdQueryHandler(IRequirementAnalysisRepository repository)
    : IRequestHandler<GetRequirementAnalysisByIdQuery, BreakdownRequirementResponse?>
{
    public async Task<BreakdownRequirementResponse?> Handle(
        GetRequirementAnalysisByIdQuery request,
        CancellationToken cancellationToken)
    {
        var analysis = await repository.GetByIdAsync(request.Id, cancellationToken);
        return analysis is null ? null : RequirementMapper.ToBreakdownResponse(analysis);
    }
}
