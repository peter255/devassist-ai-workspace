namespace DevAssist.Application.Interfaces.Copilot;

public sealed record ChatCompletionRequest(string SystemPrompt, string UserPrompt);

public interface IAzureOpenAiChatService
{
    Task<string> CompleteAsync(ChatCompletionRequest request, CancellationToken cancellationToken);

    /// <summary>
    /// Streams tokens from the model one by one. Implementations that do not support streaming
    /// should yield the full response as a single token via CompleteAsync.
    /// </summary>
    IAsyncEnumerable<string> StreamAsync(ChatCompletionRequest request, CancellationToken cancellationToken);
}
