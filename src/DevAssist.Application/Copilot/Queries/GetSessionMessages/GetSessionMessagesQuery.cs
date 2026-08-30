using DevAssist.Contracts.Copilot;
using MediatR;

namespace DevAssist.Application.Copilot.Queries.GetSessionMessages;

public sealed record GetSessionMessagesQuery(Guid SessionId, Guid? UserId = null) : IRequest<IReadOnlyList<ChatMessageDto>>;
