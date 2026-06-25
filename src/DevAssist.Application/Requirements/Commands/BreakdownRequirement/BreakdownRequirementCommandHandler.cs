using DevAssist.Application.Interfaces;
using DevAssist.Application.Interfaces.Requirements;
using DevAssist.Application.Requirements.Mappings;
using DevAssist.Contracts.Requirements;
using DevAssist.Domain.Entities;
using FluentValidation;
using MediatR;

namespace DevAssist.Application.Requirements.Commands.BreakdownRequirement;

public sealed class BreakdownRequirementCommandValidator : AbstractValidator<BreakdownRequirementCommand>
{
    public BreakdownRequirementCommandValidator()
    {
        RuleFor(x => x.Text).NotEmpty().MaximumLength(12000);
    }
}

public sealed class BreakdownRequirementCommandHandler(
    IRequirementBreakdownService requirementBreakdownService,
    IRequirementAnalysisRepository requirementAnalysisRepository) : IRequestHandler<BreakdownRequirementCommand, BreakdownRequirementResponse>
{
    public async Task<BreakdownRequirementResponse> Handle(
        BreakdownRequirementCommand request,
        CancellationToken cancellationToken)
    {
        var output = await requirementBreakdownService.AnalyzeAsync(request.Text, cancellationToken);

        var analysis = new RequirementAnalysis
        {
            Id = Guid.NewGuid(),
            OriginalText = request.Text.Trim(),
            FunctionalSummary = output.FunctionalSummary,
            BackendTasksJson = RequirementMapper.ToJson(output.BackendTasks),
            FrontendTasksJson = RequirementMapper.ToJson(output.FrontendTasks),
            TestingChecklistJson = RequirementMapper.ToJson(output.TestingChecklist),
            RisksJson = RequirementMapper.ToJson(output.Risks),
            AssumptionsJson = RequirementMapper.ToJson(output.Assumptions),
            AcceptanceCriteriaJson = RequirementMapper.ToJson(output.AcceptanceCriteria),
            CreatedAt = DateTimeOffset.UtcNow
        };

        await requirementAnalysisRepository.AddAsync(analysis, cancellationToken);
        await requirementAnalysisRepository.SaveChangesAsync(cancellationToken);

        return RequirementMapper.ToBreakdownResponse(analysis);
    }
}
