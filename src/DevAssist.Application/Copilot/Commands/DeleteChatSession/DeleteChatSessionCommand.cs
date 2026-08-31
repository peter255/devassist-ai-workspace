using MediatR;

namespace DevAssist.Application.Copilot.Commands.DeleteChatSession;

public sealed record DeleteChatSessionCommand(Guid SessionId, Guid? UserId) : IRequest<Unit>;
