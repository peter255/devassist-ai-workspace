using DevAssist.Contracts.Requirements;
using MediatR;

namespace DevAssist.Application.Requirements.Queries.GetRequirementAnalysisById;

public sealed record GetRequirementAnalysisByIdQuery(Guid Id) : IRequest<BreakdownRequirementResponse?>;
