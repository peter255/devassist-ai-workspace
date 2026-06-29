using Azure;
using Azure.AI.OpenAI;
using DevAssist.Application.Interfaces;
using DevAssist.Application.Interfaces.Copilot;
using DevAssist.Infrastructure.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenAI;
using OpenAI.Chat;
using System.ClientModel;

namespace DevAssist.Infrastructure.Agents;

/// <summary>
/// AI agent backed by Azure AI Foundry / Azure OpenAI.
/// Supports both standard Azure OpenAI endpoints and Azure AI Foundry serverless
/// endpoints (which expose an OpenAI-compatible /v1 path).
///
/// Also implements IAzureOpenAiChatService for backward compatibility with existing
/// module services that were built against that interface.
/// </summary>
public sealed class AzureFoundryAgent(
    IOptions<AzureOpenAiOptions> options,
    ILogger<AzureFoundryAgent> logger) : IAiAgent, IAzureOpenAiChatService
{
    public bool IsConfigured
    {
        get
        {
            var s = options.Value;
            return !string.IsNullOrWhiteSpace(s.Endpoint)
                && !string.IsNullOrWhiteSpace(s.ApiKey)
                && !string.IsNullOrWhiteSpace(s.DeploymentName);
        }
    }

    public async Task<string> CompleteAsync(
        string systemPrompt,
        string userPrompt,
        CancellationToken cancellationToken = default)
    {
        var settings = options.Value;
        if (!IsConfigured)
            throw new InvalidOperationException(
                "AzureFoundryAgent requires AzureOpenAi:Endpoint, ApiKey, and DeploymentName to be configured.");

        try
        {
            var chatClient = CreateChatClient(settings);

            var completion = await chatClient.CompleteChatAsync(
            [
                new SystemChatMessage(systemPrompt),
                new UserChatMessage(userPrompt)
            ],
            cancellationToken: cancellationToken);

            var answer = completion.Value.Content.FirstOrDefault()?.Text;
            if (string.IsNullOrWhiteSpace(answer))
                throw new InvalidOperationException("Azure AI Foundry returned an empty response.");

            return answer;
        }
        catch (Exception ex) when (ex is not InvalidOperationException)
        {
            logger.LogError(ex, "AzureFoundryAgent chat completion failed.");
            throw;
        }
    }

    // IAzureOpenAiChatService adapter — wraps the same completion logic so that
    // legacy module services that inject IAzureOpenAiChatService continue to work
    // without modification when AzureFoundryAgent is registered.
    public Task<string> CompleteAsync(ChatCompletionRequest request, CancellationToken cancellationToken)
        => CompleteAsync(request.SystemPrompt, request.UserPrompt, cancellationToken);

    // Endpoints ending with /v1 are Azure AI Foundry / serverless OpenAI-compatible.
    // They must NOT receive an api-version query parameter (AzureOpenAIClient adds it).
    // We use the plain OpenAIClient with a trailing-slash base URL for correct path resolution.
    private static ChatClient CreateChatClient(AzureOpenAiOptions settings)
    {
        var endpoint = settings.Endpoint.TrimEnd('/');

        if (endpoint.EndsWith("/v1", StringComparison.OrdinalIgnoreCase))
        {
            var baseUrl = endpoint + "/";
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
