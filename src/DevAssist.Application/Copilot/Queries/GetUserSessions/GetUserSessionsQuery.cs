using DevAssist.Contracts.Copilot;
using MediatR;

namespace DevAssist.Application.Copilot.Queries.GetUserSessions;

public sealed record GetUserSessionsQuery(Guid UserId) : IRequest<IReadOnlyList<ChatSessionSummaryDto>>;
