using DevAssist.Application.Interfaces.Copilot;
using DevAssist.Contracts.Copilot;
using MediatR;

namespace DevAssist.Application.Copilot.Queries.GetUserSessions;

public sealed class GetUserSessionsQueryHandler(IChatRepository chatRepository)
    : IRequestHandler<GetUserSessionsQuery, IReadOnlyList<ChatSessionSummaryDto>>
{
    public async Task<IReadOnlyList<ChatSessionSummaryDto>> Handle(
        GetUserSessionsQuery request,
        CancellationToken cancellationToken)
    {
        var sessions = await chatRepository.GetSessionsByUserIdAsync(request.UserId, cancellationToken);

        return sessions
            .Select(s => new ChatSessionSummaryDto(
                s.SessionId,
                s.Title,
                s.CreatedAt,
                s.LastMessageAt,
                s.MessageCount))
            .ToList();
    }
}
