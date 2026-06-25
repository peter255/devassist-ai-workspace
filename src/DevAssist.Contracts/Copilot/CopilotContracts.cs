namespace DevAssist.Contracts.Copilot;

public sealed record CitationDto(
    Guid DocumentId,
    string DocumentName,
    string ChunkReference);

public sealed record CreateChatSessionRequest(string? Title, string CreatedBy = "system");

public sealed record CreateChatSessionResponse(
    Guid SessionId,
    string Title,
    DateTimeOffset CreatedAt);

public sealed record AskCopilotRequest(Guid SessionId, string Question);

public sealed record AskCopilotResponse(
    string Answer,
    IReadOnlyList<CitationDto> Citations);

public sealed record ChatMessageDto(
    Guid Id,
    string Role,
    string Content,
    IReadOnlyList<CitationDto>? Citations,
    DateTimeOffset CreatedAt);
