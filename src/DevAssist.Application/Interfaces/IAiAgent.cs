namespace DevAssist.Application.Interfaces;

/// <summary>
/// High-level AI completion agent abstraction.
/// Implementations: AzureFoundryAgent (Azure AI Foundry / Azure OpenAI),
/// LocalFallbackAgent (local heuristics when Azure is not configured).
/// The DI container selects the appropriate implementation at startup based on configuration.
/// </summary>
public interface IAiAgent
{
    /// <summary>
    /// True when Azure AI Foundry / Azure OpenAI credentials are configured and the agent
    /// can produce LLM-powered responses. False for the local fallback agent.
    /// </summary>
    bool IsConfigured { get; }

    /// <summary>
    /// Sends a completion request to the underlying AI backend and returns the text response.
    /// </summary>
    Task<string> CompleteAsync(
        string systemPrompt,
        string userPrompt,
        CancellationToken cancellationToken = default);
}
