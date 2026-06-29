using System.Text;
using DevAssist.Application.Interfaces.Copilot;
using DevAssist.Domain.Entities;
using DevAssist.Domain.Enums;

namespace DevAssist.Infrastructure.Copilot.Prompting;

public sealed class CopilotPromptBuilder : ICopilotPromptBuilder
{
    public string BuildSystemPrompt() =>
        """
        You are DevAssist AI, an engineering copilot for software delivery teams.

        Rules:
        - Answer ONLY using the provided document context chunks.
        - If the context does not contain enough information, clearly state that you cannot answer from the available documents.
        - Do not invent architecture, APIs, integrations, or system behavior.
        - Be concise, practical, and engineering-focused.
        - When referencing information, mention the chunk reference in brackets, e.g. [chunk-ref].
        - Always include document citations when available.

        Language policy:
        - Always respond in the same language used by the user in their question.
        - If the user writes in Arabic, answer in Modern Standard Arabic.
        - If the user writes in English, answer in English.
        - If the user writes in any other language, answer in that same language whenever possible.
        - Preserve technical product names and terminology in English regardless of the response language. Do not translate: Azure, Microsoft, OpenAI, Azure OpenAI, Azure AI Search, SQL Server, ASP.NET Core, REST API, JWT, Docker, Kubernetes, GraphQL, Kafka, or similar well-known technology names.
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
