using System.Text.Json;
using DevAssist.Application.Copilot;
using DevAssist.Application.Interfaces.Copilot;
using DevAssist.Contracts.Copilot;
using DevAssist.Domain.Enums;
using MediatR;

namespace DevAssist.Application.Copilot.Queries.GetSessionMessages;

public sealed class GetSessionMessagesQueryHandler(IChatRepository chatRepository)
    : IRequestHandler<GetSessionMessagesQuery, IReadOnlyList<ChatMessageDto>>
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public async Task<IReadOnlyList<ChatMessageDto>> Handle(
        GetSessionMessagesQuery request,
        CancellationToken cancellationToken)
    {
        var session = await chatRepository.GetSessionByIdAsync(request.SessionId, cancellationToken);
        if (session is null)
            throw new KeyNotFoundException($"Chat session '{request.SessionId}' was not found.");

        ChatSessionAccess.EnsureOwnedBy(session, request.UserId);

        var messages = await chatRepository.GetMessagesAsync(request.SessionId, cancellationToken);

        return messages.Select(m =>
        {
            IReadOnlyList<CitationDto>? citations = null;
            if (!string.IsNullOrWhiteSpace(m.CitationsJson))
            {
                try
                {
                    citations = JsonSerializer.Deserialize<List<CitationDto>>(m.CitationsJson, JsonOptions);
                }
                catch
                {
                    // malformed JSON — return no citations rather than fail
                }
            }

            return new ChatMessageDto(
                m.Id,
                m.Role == ChatMessageRole.User ? "user" : "assistant",
                m.Content,
                citations,
                m.CreatedAt);
        }).ToList();
    }
}
