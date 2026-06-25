using DevAssist.Contracts.Tickets;
using DevAssist.Domain.Entities;
using DevAssist.Domain.Enums;

namespace DevAssist.Application.Tickets.Mappings;

public static class TicketMapper
{
    public static AnalyzeTicketResponse ToAnalyzeResponse(TicketAnalysis analysis) =>
        new(
            analysis.Id,
            analysis.Summary,
            analysis.Severity.ToString(),
            analysis.Category,
            analysis.ImpactedModule,
            analysis.SuggestedAction,
            analysis.CreatedAt);

    public static TicketAnalysisListItemDto ToListItem(TicketAnalysis analysis) =>
        new(
            analysis.Id,
            analysis.Summary,
            analysis.Severity.ToString(),
            analysis.Category,
            analysis.ImpactedModule,
            analysis.SuggestedAction,
            analysis.CreatedAt);

    public static TicketSeverity ParseSeverity(string severity) =>
        Enum.TryParse<TicketSeverity>(severity, ignoreCase: true, out var parsed)
            ? parsed
            : TicketSeverity.Medium;
}
