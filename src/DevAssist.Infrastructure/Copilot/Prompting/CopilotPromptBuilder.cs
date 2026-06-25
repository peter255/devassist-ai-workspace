using System.Text;
using DevAssist.Application.Interfaces.Copilot;
using DevAssist.Domain.Entities;
using DevAssist.Domain.Enums;

namespace DevAssist.Infrastructure.Copilot.Prompting;

public sealed class CopilotPromptBuilder : ICopilotPromptBuilder
{
    public string BuildSystemPrompt() =>
        """
        You are DevAssist Knowledge Copilot, an internal assistant for software delivery teams.
        Rules:
        - Answer ONLY using the provided document context chunks.
        - If the context does not contain enough information, clearly state that you cannot answer from the available documents.
        - Do not invent architecture, APIs, integrations, or system behavior.
        - Be concise, practical, and engineering-focused.
        - When referencing information, mention the chunk reference in brackets, e.g. [chunk-ref].
        """;

    public string BuildUserPrompt(string question, IReadOnlyList<RetrievedChunk> chunks, IReadOnlyList<ChatMessage> history)
    {
        var builder = new StringBuilder();

        if (history.Count > 0)
        {
            builder.AppendLine("Recent conversation:");
            foreach (var message in history.TakeLast(6))
            {
                var role = message.Role == ChatMessageRole.User ? "User" : "Assistant";
                builder.AppendLine($"{role}: {message.Content}");
            }
            builder.AppendLine();
        }

        if (chunks.Count == 0)
        {
            builder.AppendLine("No document context was retrieved.");
        }
        else
        {
            builder.AppendLine("Document context:");
            foreach (var chunk in chunks)
            {
                builder.AppendLine($"[{chunk.ChunkReference}]");
                builder.AppendLine($"Document: {chunk.DocumentName} ({chunk.DocumentType})");
                builder.AppendLine(chunk.Content);
                builder.AppendLine("---");
            }
        }

        builder.AppendLine();
        builder.AppendLine($"Question: {question}");
        return builder.ToString();
    }
}
