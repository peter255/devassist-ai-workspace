using DevAssist.Contracts.Requirements;
using MediatR;

namespace DevAssist.Application.Requirements.Queries.GetRequirementAnalyses;

public sealed record GetRequirementAnalysesQuery(int Limit = 20) : IRequest<IReadOnlyList<RequirementAnalysisListItemDto>>;
