using System.Text.Json;
using DevAssist.Application.Interfaces;
using DevAssist.Application.Interfaces.Copilot;
using DevAssist.Application.Interfaces.Requirements;
using Microsoft.Extensions.Logging;

namespace DevAssist.Infrastructure.Requirements.Analysis;

internal sealed record RequirementBreakdownJson(
    string FunctionalSummary,
    IReadOnlyList<string> BackendTasks,
    IReadOnlyList<string> FrontendTasks,
    IReadOnlyList<string> TestingChecklist,
    IReadOnlyList<string> Risks,
    IReadOnlyList<string> Assumptions,
    IReadOnlyList<string> AcceptanceCriteria);

public sealed class AzureOpenAiRequirementBreakdownService(
    IAzureOpenAiChatService chatService,
    IRequirementBreakdownPromptBuilder promptBuilder,
    ILogger<AzureOpenAiRequirementBreakdownService> logger) : IRequirementBreakdownService
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public async Task<RequirementBreakdownOutput> AnalyzeAsync(string text, CancellationToken cancellationToken)
    {
        var raw = await chatService.CompleteAsync(
            new ChatCompletionRequest(promptBuilder.BuildSystemPrompt(), promptBuilder.BuildUserPrompt(text)),
            cancellationToken);

        return ParseOutput(raw, logger);
    }

    internal static RequirementBreakdownOutput ParseOutput(string raw, ILogger logger)
    {
        var json = ExtractJson(raw);
        try
        {
            var parsed = JsonSerializer.Deserialize<RequirementBreakdownJson>(json, JsonOptions)
                ?? throw new InvalidOperationException("Requirement breakdown JSON was empty.");

            return new RequirementBreakdownOutput(
                parsed.FunctionalSummary,
                NormalizeList(parsed.BackendTasks),
                NormalizeList(parsed.FrontendTasks),
                NormalizeList(parsed.TestingChecklist),
                NormalizeList(parsed.Risks),
                NormalizeList(parsed.Assumptions),
                NormalizeList(parsed.AcceptanceCriteria));
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to parse requirement breakdown JSON. Raw response: {Raw}", raw);
            throw new InvalidOperationException("Failed to parse structured requirement breakdown from the model response.");
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

    private static IReadOnlyList<string> NormalizeList(IReadOnlyList<string>? items) =>
        items?.Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim()).ToList() ?? [];
}

public sealed class LocalRequirementBreakdownService(
    IRequirementBreakdownPromptBuilder promptBuilder,
    ILogger<LocalRequirementBreakdownService> logger) : IRequirementBreakdownService
{
    public Task<RequirementBreakdownOutput> AnalyzeAsync(string text, CancellationToken cancellationToken)
    {
        logger.LogInformation("Using local heuristic requirement breakdown (Azure OpenAI not configured).");

        var normalized = text.Trim();
        var lower = normalized.ToLowerInvariant();

        var firstSentence = normalized.Split(['.', '\n'], StringSplitOptions.RemoveEmptyEntries).FirstOrDefault()?.Trim()
            ?? normalized;
        var functionalSummary = firstSentence.Length > 300 ? firstSentence[..300] + "…" : firstSentence;

        var isAuth = lower.Contains("otp") || lower.Contains("login") || lower.Contains("auth");
        var isUpload = lower.Contains("upload") || lower.Contains("document") || lower.Contains("file");
        var isApi = lower.Contains("api") || lower.Contains("endpoint") || lower.Contains("integration");

        var backendTasks = new List<string>
        {
            "Define domain models and persistence changes for the new capability",
            "Implement service layer logic with validation and error handling",
            "Expose REST API endpoints with appropriate authorization"
        };

        if (isAuth)
        {
            backendTasks.Insert(0, "Implement authentication flow services (token/OTP generation, verification, lockout policy)");
            backendTasks.Add("Add audit logging for security-sensitive events");
        }

        if (isUpload)
        {
            backendTasks.Insert(1, "Integrate blob storage upload pipeline and metadata persistence");
            backendTasks.Add("Wire indexing pipeline for uploaded content chunks");
        }

        if (isApi)
        {
            backendTasks.Add("Document API contracts and update integration tests");
        }

        var frontendTasks = new List<string>
        {
            "Design and implement UI flow for the primary user journey",
            "Add loading, error, and empty states for all new screens",
            "Integrate with backend APIs and handle validation feedback"
        };

        if (isAuth)
        {
            frontendTasks.Insert(0, "Add multi-step auth UI (input, verification, lockout messaging)");
        }

        if (isUpload)
        {
            frontendTasks.Insert(0, "Build drag-and-drop upload component with progress indicator");
        }

        var testingChecklist = new List<string>
        {
            "Happy-path end-to-end flow completes successfully",
            "Invalid input shows clear validation errors",
            "Authorization rules are enforced for protected actions",
            "Regression test on related existing flows"
        };

        if (isAuth)
        {
            testingChecklist.Add("Account lockout triggers after configured failed attempts");
            testingChecklist.Add("OTP/session expiry is handled gracefully");
        }

        var risks = new List<string>
        {
            "Scope creep if edge cases are not bounded early",
            "Cross-team dependencies may delay integration"
        };

        if (isAuth)
        {
            risks.Add("Security thresholds for brute-force protection need explicit definition");
        }

        if (isUpload)
        {
            risks.Add("Large file handling and storage costs need limits");
        }

        var assumptions = new List<string>
        {
            "Existing CI/CD pipeline can deploy backend and frontend changes",
            "Team has access to required environments for integration testing"
        };

        if (isAuth)
        {
            assumptions.Add("SMS or email delivery provider integration exists or will be provisioned separately");
        }

        var acceptanceCriteria = new List<string>
        {
            "Primary user flow works in staging with realistic test data",
            "API responses match documented contracts",
            "Key scenarios are covered by automated tests"
        };

        if (isAuth)
        {
            acceptanceCriteria.Add("User can complete authentication with valid credentials/OTP");
            acceptanceCriteria.Add("System enforces lockout policy after repeated failures");
        }

        _ = promptBuilder.BuildSystemPrompt();

        return Task.FromResult(new RequirementBreakdownOutput(
            functionalSummary,
            backendTasks,
            frontendTasks,
            testingChecklist,
            risks,
            assumptions,
            acceptanceCriteria));
    }
}
