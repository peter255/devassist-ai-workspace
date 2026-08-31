using DevAssist.Application.Copilot;
using DevAssist.Application.Interfaces.Copilot;
using MediatR;

namespace DevAssist.Application.Copilot.Commands.DeleteChatSession;

public sealed class DeleteChatSessionCommandHandler(IChatRepository chatRepository)
    : IRequestHandler<DeleteChatSessionCommand, Unit>
{
    public async Task<Unit> Handle(DeleteChatSessionCommand request, CancellationToken cancellationToken)
    {
        var session = await chatRepository.GetSessionByIdAsync(request.SessionId, cancellationToken);
        if (session is null)
            throw new KeyNotFoundException($"Chat session '{request.SessionId}' was not found.");

        ChatSessionAccess.EnsureOwnedBy(session, request.UserId);

        await chatRepository.DeleteSessionAsync(session, cancellationToken);
        await chatRepository.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}
