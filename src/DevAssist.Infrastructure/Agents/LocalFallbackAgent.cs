using DevAssist.Application.Interfaces;
using DevAssist.Application.Interfaces.Copilot;
using Microsoft.Extensions.Logging;

namespace DevAssist.Infrastructure.Agents;

/// <summary>
/// Local fallback AI agent used when Azure AI Foundry / Azure OpenAI is not configured.
///
/// For the Knowledge Copilot the response is grounded in retrieved document chunks so the
/// application remains useful even without an LLM. Ticket and requirement analysis fall back
/// to rule-based heuristics in their respective local service implementations.
///
/// Also implements IAzureOpenAiChatService for backward compatibility.
/// </summary>
public sealed class LocalFallbackAgent(ILogger<LocalFallbackAgent> logger) : IAiAgent, IAzureOpenAiChatService
{
    public bool IsConfigured => false;

    public Task<string> CompleteAsync(
        string systemPrompt,
        string userPrompt,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation(
            "LocalFallbackAgent invoked — Azure AI Foundry is not configured. " +
            "Returning empty response so callers apply module-specific fallback logic.");

        // Return empty string; callers (KnowledgeCopilotService, LocalGroundedChatService)
        // detect empty and apply their own grounding or heuristic response.
        return Task.FromResult(string.Empty);
    }

    // IAzureOpenAiChatService adapter for backward compatibility.
    // LocalGroundedChatService delegates its grounding logic here only for copilot prompts;
    // other callers receive an empty string and apply their own fallback.
    public Task<string> CompleteAsync(ChatCompletionRequest request, CancellationToken cancellationToken)
        => CompleteAsync(request.SystemPrompt, request.UserPrompt, cancellationToken);
}
