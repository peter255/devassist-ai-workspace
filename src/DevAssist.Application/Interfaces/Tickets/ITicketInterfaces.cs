namespace DevAssist.Application.Interfaces.Tickets;

public sealed record TicketAnalysisOutput(
    string Summary,
    string Severity,
    string Category,
    string ImpactedModule,
    string SuggestedAction);

public interface ITicketAnalysisRepository
{
    Task AddAsync(Domain.Entities.TicketAnalysis analysis, CancellationToken cancellationToken);
    Task<IReadOnlyList<Domain.Entities.TicketAnalysis>> GetRecentAsync(int limit, CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}

public interface ITicketAnalyzerPromptBuilder
{
    string BuildSystemPrompt();
    string BuildUserPrompt(string ticketText);
}
