namespace DevAssist.Application.Interfaces.Copilot;

public sealed record ChatCompletionRequest(string SystemPrompt, string UserPrompt);

public interface IAzureOpenAiChatService
{
    Task<string> CompleteAsync(ChatCompletionRequest request, CancellationToken cancellationToken);
}
