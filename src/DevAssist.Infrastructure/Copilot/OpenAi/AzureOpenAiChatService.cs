using Azure;
using Azure.AI.OpenAI;
using DevAssist.Application.Interfaces.Copilot;
using DevAssist.Infrastructure.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenAI.Chat;

namespace DevAssist.Infrastructure.Copilot.OpenAi;

public sealed class AzureOpenAiChatService(
    IOptions<AzureOpenAiOptions> options,
    ILogger<AzureOpenAiChatService> logger) : IAzureOpenAiChatService
{
    public async Task<string> CompleteAsync(ChatCompletionRequest request, CancellationToken cancellationToken)
    {
        var settings = options.Value;
        if (string.IsNullOrWhiteSpace(settings.Endpoint) || string.IsNullOrWhiteSpace(settings.ApiKey))
        {
            throw new InvalidOperationException("Azure OpenAI is not configured.");
        }

        if (string.IsNullOrWhiteSpace(settings.DeploymentName))
        {
            throw new InvalidOperationException("Azure OpenAI deployment name is not configured.");
        }

        try
        {
            var client = new AzureOpenAIClient(new Uri(settings.Endpoint), new AzureKeyCredential(settings.ApiKey));
            var chatClient = client.GetChatClient(settings.DeploymentName);

            var completion = await chatClient.CompleteChatAsync(
            [
                new SystemChatMessage(request.SystemPrompt),
                new UserChatMessage(request.UserPrompt)
            ],
            cancellationToken: cancellationToken);

            var answer = completion.Value.Content.FirstOrDefault()?.Text;

            if (string.IsNullOrWhiteSpace(answer))
            {
                throw new InvalidOperationException("Azure OpenAI returned an empty response.");
            }

            return answer;
        }
        catch (Exception ex) when (ex is not InvalidOperationException)
        {
            logger.LogError(ex, "Azure OpenAI chat completion failed.");
            throw;
        }
    }
}

public sealed class LocalGroundedChatService(ILogger<LocalGroundedChatService> logger) : IAzureOpenAiChatService
{
    public Task<string> CompleteAsync(ChatCompletionRequest request, CancellationToken cancellationToken)
    {
        logger.LogInformation("Using local grounded chat fallback (Azure OpenAI not configured).");

        if (request.UserPrompt.Contains("No document context was retrieved.", StringComparison.Ordinal))
        {
            return Task.FromResult(
                "I could not find relevant information in the indexed documents to answer this question. " +
                "Please upload and index engineering documents first, or try rephrasing your question.");
        }

        var contextSection = ExtractSection(request.UserPrompt, "Document context:", "Question:");
        if (string.IsNullOrWhiteSpace(contextSection))
        {
            return Task.FromResult(
                "I do not have enough indexed document context to provide a grounded answer.");
        }

        var question = ExtractSection(request.UserPrompt, "Question:", null).Trim();
        var summary = contextSection.Length > 1200 ? contextSection[..1200] + "…" : contextSection;

        var answer =
            $"Based on the retrieved document context, here is a grounded summary for your question \"{question}\":\n\n" +
            summary.Trim() +
            "\n\n(Generated locally without Azure OpenAI — configure Azure OpenAI for richer answers.)";

        return Task.FromResult(answer);
    }

    private static string ExtractSection(string text, string startMarker, string? endMarker)
    {
        var start = text.IndexOf(startMarker, StringComparison.Ordinal);
        if (start < 0) return string.Empty;

        start += startMarker.Length;
        if (endMarker is null)
        {
            return text[start..].Trim();
        }

        var end = text.IndexOf(endMarker, start, StringComparison.Ordinal);
        return end < 0 ? text[start..].Trim() : text[start..end].Trim();
    }
}
