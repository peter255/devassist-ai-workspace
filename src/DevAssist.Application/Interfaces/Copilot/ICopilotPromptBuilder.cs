using DevAssist.Domain.Entities;

namespace DevAssist.Application.Interfaces.Copilot;

public interface ICopilotPromptBuilder
{
    string BuildSystemPrompt();
    string BuildUserPrompt(string question, IReadOnlyList<RetrievedChunk> chunks, IReadOnlyList<ChatMessage> history);
}
