namespace DevAssist.Application.Copilot;

public sealed record ChatSessionSummary(
    Guid SessionId,
    string Title,
    DateTimeOffset CreatedAt,
    DateTimeOffset? LastMessageAt,
    int MessageCount);
