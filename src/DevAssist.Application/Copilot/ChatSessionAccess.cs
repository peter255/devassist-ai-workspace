using DevAssist.Domain.Entities;

namespace DevAssist.Application.Copilot;

public static class ChatSessionAccess
{
    public static void EnsureOwnedBy(ChatSession session, Guid? userId)
    {
        if (userId is null)
            return;

        if (session.UserId != userId)
            throw new UnauthorizedAccessException("You do not have access to this chat session.");
    }
}
