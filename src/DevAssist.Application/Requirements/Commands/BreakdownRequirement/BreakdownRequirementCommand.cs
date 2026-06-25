using DevAssist.Contracts.Requirements;
using MediatR;

namespace DevAssist.Application.Requirements.Commands.BreakdownRequirement;

public sealed record BreakdownRequirementCommand(string Text) : IRequest<BreakdownRequirementResponse>;
