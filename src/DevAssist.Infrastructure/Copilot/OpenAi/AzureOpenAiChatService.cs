using Azure;
using Azure.AI.OpenAI;
using DevAssist.Application.Interfaces.Copilot;
using DevAssist.Infrastructure.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenAI;
using OpenAI.Chat;
using System.ClientModel;

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
            var chatClient = CreateChatClient(settings);

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

    // Endpoints ending with /v1 (Azure AI Foundry / serverless deployments) use the
    // OpenAI-compatible REST surface which does NOT accept an api-version query parameter.
    // Standard Azure OpenAI endpoints use AzureOpenAIClient which appends api-version.
    // Endpoints ending with /v1 are Azure AI Foundry / serverless OpenAI-compatible endpoints.
    // They must NOT receive an api-version query parameter (AzureOpenAIClient adds it automatically).
    // We use OpenAIClient with a trailing-slash base URL so the SDK builds paths correctly:
    //   https://resource.openai.azure.com/openai/v1/  +  chat/completions
    // Without the trailing slash, .NET URI resolution drops "v1" from the path.
    private static ChatClient CreateChatClient(AzureOpenAiOptions settings)
    {
        var endpoint = settings.Endpoint.TrimEnd('/');
        if (endpoint.EndsWith("/v1", StringComparison.OrdinalIgnoreCase))
        {
            var baseUrl = endpoint + "/";   // trailing slash required for correct path resolution
            var clientOptions = new OpenAIClientOptions { Endpoint = new Uri(baseUrl) };
            var client = new OpenAIClient(new ApiKeyCredential(settings.ApiKey), clientOptions);
            return client.GetChatClient(settings.DeploymentName);
        }

        var azureClient = new AzureOpenAIClient(
            new Uri(endpoint + "/"),
            new AzureKeyCredential(settings.ApiKey));
        return azureClient.GetChatClient(settings.DeploymentName);
    }
}

public sealed class LocalGroundedChatService(ILogger<LocalGroundedChatService> logger) : IAzureOpenAiChatService
{
    public Task<string> CompleteAsync(ChatCompletionRequest request, CancellationToken cancellationToken)
    {
        logger.LogInformation("Using local grounded chat fallback (Azure OpenAI not configured).");

        // Only handle prompts in the copilot format produced by CopilotPromptBuilder.
        // Non-copilot requests (e.g. the query-translation prompt from KnowledgeCopilotService)
        // receive an empty response so the caller can apply its own fallback logic.
        var isCopilotPrompt =
            request.UserPrompt.Contains("Question:", StringComparison.Ordinal) ||
            request.UserPrompt.Contains("Document context:", StringComparison.Ordinal) ||
            request.UserPrompt.Contains("No document context was retrieved.", StringComparison.Ordinal);

        if (!isCopilotPrompt)
            return Task.FromResult(string.Empty);

        var question = ExtractSection(request.UserPrompt, "Question:", null).Trim();
        var arabic = IsPrimarilyNonLatin(question);

        if (request.UserPrompt.Contains("No document context was retrieved.", StringComparison.Ordinal))
        {
            var msg = arabic
                ? "لم أتمكن من العثور على معلومات ذات صلة في المستندات المفهرسة للإجابة على هذا السؤال. " +
                  "يرجى تحميل وفهرسة المستندات الهندسية أولاً، أو حاول إعادة صياغة سؤالك."
                : "I could not find relevant information in the indexed documents to answer this question. " +
                  "Please upload and index engineering documents first, or try rephrasing your question.";
            return Task.FromResult(msg);
        }

        var contextSection = ExtractSection(request.UserPrompt, "Document context:", "Question:");
        if (string.IsNullOrWhiteSpace(contextSection))
        {
            var msg = arabic
                ? "لا يتوفر سياق كافٍ من المستندات المفهرسة لتقديم إجابة موثوقة."
                : "I do not have enough indexed document context to provide a grounded answer.";
            return Task.FromResult(msg);
        }

        var summary = contextSection.Length > 1200 ? contextSection[..1200] + "…" : contextSection;

        var answer = arabic
            ? $"استناداً إلى سياق المستند المسترجع، فيما يلي ملخص لسؤالك \"{question}\":\n\n" +
              summary.Trim() +
              "\n\n(تم الإنشاء محلياً دون Azure OpenAI — يرجى تهيئة Azure OpenAI للحصول على إجابات أكثر دقةً وثراءً.)"
            : $"Based on the retrieved document context, here is a grounded summary for your question \"{question}\":\n\n" +
              summary.Trim() +
              "\n\n(Generated locally without Azure OpenAI — configure Azure OpenAI for richer answers.)";

        return Task.FromResult(answer);
    }

    // Mirrors the same logic in KnowledgeCopilotService to avoid a shared dependency.
    private static bool IsPrimarilyNonLatin(string text)
    {
        var letters = text.Where(char.IsLetter).ToList();
        if (letters.Count == 0) return false;
        return letters.Count(c => c > '\u024F') > letters.Count / 2;
    }

    private static string ExtractSection(string text, string startMarker, string? endMarker)
    {
        var start = text.IndexOf(startMarker, StringComparison.Ordinal);
        if (start < 0) return string.Empty;

        start += startMarker.Length;
        if (endMarker is null)
            return text[start..].Trim();

        var end = text.IndexOf(endMarker, start, StringComparison.Ordinal);
        return end < 0 ? text[start..].Trim() : text[start..end].Trim();
    }
}
