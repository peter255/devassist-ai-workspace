namespace DevAssist.Contracts.Requirements;

public sealed record BreakdownRequirementRequest(string Text);

public sealed record BreakdownRequirementResponse(
    Guid Id,
    string FunctionalSummary,
    IReadOnlyList<string> BackendTasks,
    IReadOnlyList<string> FrontendTasks,
    IReadOnlyList<string> TestingChecklist,
    IReadOnlyList<string> Risks,
    IReadOnlyList<string> Assumptions,
    IReadOnlyList<string> AcceptanceCriteria,
    DateTimeOffset CreatedAt);

public sealed record RequirementAnalysisListItemDto(
    Guid Id,
    string FunctionalSummary,
    DateTimeOffset CreatedAt);
