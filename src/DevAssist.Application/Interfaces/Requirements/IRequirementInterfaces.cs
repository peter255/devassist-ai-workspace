namespace DevAssist.Application.Interfaces.Requirements;

public sealed record RequirementBreakdownOutput(
    string FunctionalSummary,
    IReadOnlyList<string> BackendTasks,
    IReadOnlyList<string> FrontendTasks,
    IReadOnlyList<string> TestingChecklist,
    IReadOnlyList<string> Risks,
    IReadOnlyList<string> Assumptions,
    IReadOnlyList<string> AcceptanceCriteria);

public interface IRequirementAnalysisRepository
{
    Task AddAsync(Domain.Entities.RequirementAnalysis analysis, CancellationToken cancellationToken);
    Task<Domain.Entities.RequirementAnalysis?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<IReadOnlyList<Domain.Entities.RequirementAnalysis>> GetRecentAsync(int limit, CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}

public interface IRequirementBreakdownPromptBuilder
{
    string BuildSystemPrompt();
    string BuildUserPrompt(string requirementText);
}
