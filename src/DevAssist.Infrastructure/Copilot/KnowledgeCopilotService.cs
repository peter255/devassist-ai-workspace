using System.Text.Json;
using DevAssist.Application.Interfaces;
using DevAssist.Application.Interfaces.Copilot;
using DevAssist.Contracts.Copilot;
using DevAssist.Domain.Entities;
using DevAssist.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace DevAssist.Infrastructure.Copilot;

public sealed class KnowledgeCopilotService(
    IChatRepository chatRepository,
    IDocumentSearchRetriever searchRetriever,
    ICopilotPromptBuilder promptBuilder,
    IAzureOpenAiChatService chatService,
    ILogger<KnowledgeCopilotService> logger) : IKnowledgeCopilotService
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public async Task<AskCopilotResponse> AskAsync(Guid sessionId, string question, CancellationToken cancellationToken)
    {
        var session = await chatRepository.GetSessionByIdAsync(sessionId, cancellationToken)
            ?? throw new KeyNotFoundException($"Chat session '{sessionId}' was not found.");

        var userMessage = new ChatMessage
        {
            Id = Guid.NewGuid(),
            ChatSessionId = session.Id,
            Role = ChatMessageRole.User,
            Content = question.Trim(),
            CreatedAt = DateTimeOffset.UtcNow
        };
        await chatRepository.AddMessageAsync(userMessage, cancellationToken);
        await chatRepository.SaveChangesAsync(cancellationToken);

        var history = await chatRepository.GetMessagesAsync(sessionId, cancellationToken);
        var chunks = await searchRetriever.SearchAsync(question, top: 5, cancellationToken);

        var citations = chunks.Select(c => new CitationDto(c.DocumentId, c.DocumentName, c.ChunkReference)).ToList();

        var systemPrompt = promptBuilder.BuildSystemPrompt();
        var userPrompt = promptBuilder.BuildUserPrompt(question, chunks, history);

        string answer;
        try
        {
            answer = await chatService.CompleteAsync(new ChatCompletionRequest(systemPrompt, userPrompt), cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Copilot chat completion failed for session {SessionId}.", sessionId);
            answer = chunks.Count == 0
                ? "I could not retrieve relevant document context to answer your question."
                : "An error occurred while generating the answer. Please try again.";
        }

        var citationsJson = citations.Count > 0 ? JsonSerializer.Serialize(citations, JsonOptions) : null;

        var assistantMessage = new ChatMessage
        {
            Id = Guid.NewGuid(),
            ChatSessionId = session.Id,
            Role = ChatMessageRole.Assistant,
            Content = answer,
            CitationsJson = citationsJson,
            CreatedAt = DateTimeOffset.UtcNow
        };
        await chatRepository.AddMessageAsync(assistantMessage, cancellationToken);
        await chatRepository.SaveChangesAsync(cancellationToken);

        return new AskCopilotResponse(answer, citations);
    }
}
