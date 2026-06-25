using System.Text.Json;
using DevAssist.Application.Interfaces;
using DevAssist.Application.Interfaces.Copilot;
using DevAssist.Application.Interfaces.Tickets;
using Microsoft.Extensions.Logging;

namespace DevAssist.Infrastructure.Tickets.Analysis;

internal sealed record TicketAnalysisJson(
    string Summary,
    string Severity,
    string Category,
    string ImpactedModule,
    string SuggestedAction);

public sealed class AzureOpenAiTicketAnalyzerService(
    IAzureOpenAiChatService chatService,
    ITicketAnalyzerPromptBuilder promptBuilder,
    ILogger<AzureOpenAiTicketAnalyzerService> logger) : ITicketAnalyzerService
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public async Task<TicketAnalysisOutput> AnalyzeAsync(string text, CancellationToken cancellationToken)
    {
        var raw = await chatService.CompleteAsync(
            new ChatCompletionRequest(promptBuilder.BuildSystemPrompt(), promptBuilder.BuildUserPrompt(text)),
            cancellationToken);

        return ParseOutput(raw, logger);
    }

    internal static TicketAnalysisOutput ParseOutput(string raw, ILogger logger)
    {
        var json = ExtractJson(raw);
        try
        {
            var parsed = JsonSerializer.Deserialize<TicketAnalysisJson>(json, JsonOptions)
                ?? throw new InvalidOperationException("Ticket analysis JSON was empty.");

            return new TicketAnalysisOutput(
                parsed.Summary,
                NormalizeSeverity(parsed.Severity),
                parsed.Category,
                parsed.ImpactedModule,
                parsed.SuggestedAction);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to parse ticket analysis JSON. Raw response: {Raw}", raw);
            throw new InvalidOperationException("Failed to parse structured ticket analysis from the model response.");
        }
    }

    private static string ExtractJson(string raw)
    {
        var start = raw.IndexOf('{');
        var end = raw.LastIndexOf('}');
        if (start >= 0 && end > start)
        {
            return raw[start..(end + 1)];
        }

        return raw;
    }

    private static string NormalizeSeverity(string severity) =>
        severity.Trim() switch
        {
            var s when s.Equals("Critical", StringComparison.OrdinalIgnoreCase) => "Critical",
            var s when s.Equals("High", StringComparison.OrdinalIgnoreCase) => "High",
            var s when s.Equals("Low", StringComparison.OrdinalIgnoreCase) => "Low",
            _ => "Medium"
        };
}

public sealed class LocalTicketAnalyzerService(
    ITicketAnalyzerPromptBuilder promptBuilder,
    ILogger<LocalTicketAnalyzerService> logger) : ITicketAnalyzerService
{
    public Task<TicketAnalysisOutput> AnalyzeAsync(string text, CancellationToken cancellationToken)
    {
        logger.LogInformation("Using local heuristic ticket analyzer (Azure OpenAI not configured).");

        var normalized = text.Trim();
        var lower = normalized.ToLowerInvariant();

        var severity = "Medium";
        if (lower.Contains("critical") || lower.Contains("outage") || lower.Contains("data loss") || lower.Contains("security breach"))
        {
            severity = "Critical";
        }
        else if (lower.Contains("500") || lower.Contains("production") || lower.Contains("cannot") || lower.Contains("blocked"))
        {
            severity = "High";
        }
        else if (lower.Contains("cosmetic") || lower.Contains("typo") || lower.Contains("minor"))
        {
            severity = "Low";
        }

        var category = "General";
        if (lower.Contains("auth") || lower.Contains("login") || lower.Contains("logout") || lower.Contains("session"))
        {
            category = "Authentication / Session Management";
        }
        else if (lower.Contains("api") || lower.Contains("endpoint"))
        {
            category = "API / Backend";
        }
        else if (lower.Contains("ui") || lower.Contains("button") || lower.Contains("screen"))
        {
            category = "Frontend / UI";
        }
        else if (lower.Contains("database") || lower.Contains("sql"))
        {
            category = "Database";
        }

        var impactedModule = category switch
        {
            "Authentication / Session Management" => "Auth service / session handling",
            "API / Backend" => "Backend API layer",
            "Frontend / UI" => "Web client / UI components",
            "Database" => "Database / persistence layer",
            _ => "Unspecified module — review logs and ownership"
        };

        var firstSentence = normalized.Split(['.', '\n'], StringSplitOptions.RemoveEmptyEntries).FirstOrDefault()?.Trim()
            ?? normalized;
        var summary = firstSentence.Length > 250 ? firstSentence[..250] + "…" : firstSentence;

        var suggestedAction = severity switch
        {
            "Critical" => "Escalate immediately, capture logs, and assess rollback or hotfix options.",
            "High" => "Reproduce in staging, inspect recent deployments, and assign an owner for same-day investigation.",
            _ => "Review related service logs, confirm reproduction steps, and validate the affected user flow."
        };

        // Touch prompt builder so DI graph stays consistent in local mode.
        _ = promptBuilder.BuildSystemPrompt();

        return Task.FromResult(new TicketAnalysisOutput(summary, severity, category, impactedModule, suggestedAction));
    }
}
