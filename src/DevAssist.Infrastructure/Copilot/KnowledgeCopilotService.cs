using System.Text.Json;
using System.Text.RegularExpressions;
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

    // Characters above U+024F are outside the Latin/Latin-Extended blocks.
    // A question with more than 50% such letters is treated as non-Latin script.
    private static bool IsPrimarilyNonLatin(string text)
    {
        var letters = text.Where(char.IsLetter).ToList();
        if (letters.Count == 0) return false;
        var nonLatin = letters.Count(c => c > '\u024F');
        return (double)nonLatin / letters.Count > 0.5;
    }

    // Extracts sequences of Latin-script words from a question.
    // These are typically English technical terms (Azure, JWT, SQL Server, etc.)
    // embedded in non-Latin questions, and they match English document chunks directly.
    private static readonly Regex LatinTermRegex =
        new(@"[A-Za-z][A-Za-z0-9]*(?:[.\-][A-Za-z0-9]+)*(?:\s+[A-Za-z][A-Za-z0-9]*(?:[.\-][A-Za-z0-9]+)*)*",
            RegexOptions.Compiled);

    private static string ExtractLatinTerms(string text)
    {
        var terms = LatinTermRegex.Matches(text)
            .Select(m => m.Value.Trim())
            .Where(t => t.Length > 1)
            .ToList();
        return string.Join(" ", terms);
    }

    // Translates a non-Latin question into a concise English keyword query for retrieval only.
    // Strategy (in priority order):
    //   1. LLM translation via chatService (works when Azure OpenAI is configured).
    //   2. Latin terms already embedded in the question (e.g. "Azure AI Search" inside an Arabic sentence).
    //   3. Original question as last resort.
    private async Task<string> BuildEnglishSearchQueryAsync(string question, CancellationToken cancellationToken)
    {
        const string systemPrompt =
            "You are a search query translator. " +
            "Read the user's question and return a concise English keyword search query (maximum 10 words) " +
            "that captures the core topic. " +
            "Return ONLY the search query text — no explanation, no punctuation at the end.";
        try
        {
            var result = await chatService.CompleteAsync(
                new ChatCompletionRequest(systemPrompt, question),
                cancellationToken);

            // Accept only non-empty results; LocalGroundedChatService returns empty
            // for non-copilot prompts so it falls through to the Latin-term stage.
            if (!string.IsNullOrWhiteSpace(result))
            {
                logger.LogInformation("LLM translated search query: {Query}", result.Trim());
                return result.Trim();
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Search query translation via LLM failed; trying Latin-term extraction.");
        }

        // Fall back to any Latin-script technical terms already present in the question.
        var latinTerms = ExtractLatinTerms(question);
        if (!string.IsNullOrWhiteSpace(latinTerms))
        {
            logger.LogInformation("Using embedded Latin terms as search query: {Terms}", latinTerms);
            return latinTerms;
        }

        logger.LogWarning("Could not build English search query; using original question for retrieval.");
        return question;
    }

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

        // Translate non-Latin queries to English for retrieval so that Arabic (or other
        // non-Latin-script) questions can match English document chunks. The original
        // question is preserved unchanged in the user prompt so the language policy applies.
        var searchQuery = IsPrimarilyNonLatin(question.Trim())
            ? await BuildEnglishSearchQueryAsync(question.Trim(), cancellationToken)
            : question;

        if (!ReferenceEquals(searchQuery, question))
            logger.LogInformation("Non-Latin query detected; using translated search query for retrieval.");

        var chunks = await searchRetriever.SearchAsync(searchQuery, top: 5, cancellationToken);

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
