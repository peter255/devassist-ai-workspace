namespace DevAssist.Contracts.Tickets;

public sealed record AnalyzeTicketRequest(string Text);

public sealed record AnalyzeTicketResponse(
    Guid Id,
    string Summary,
    string Severity,
    string Category,
    string ImpactedModule,
    string SuggestedAction,
    DateTimeOffset CreatedAt);

public sealed record TicketAnalysisListItemDto(
    Guid Id,
    string Summary,
    string Severity,
    string Category,
    string ImpactedModule,
    string SuggestedAction,
    DateTimeOffset CreatedAt);
