using DevAssist.Domain.Entities;

namespace DevAssist.Application.Interfaces.Copilot;

public interface IChatRepository
{
    Task<ChatSession?> GetSessionByIdAsync(Guid sessionId, CancellationToken cancellationToken);
    Task<ChatSession> CreateSessionAsync(ChatSession session, CancellationToken cancellationToken);
    Task AddMessageAsync(ChatMessage message, CancellationToken cancellationToken);
    Task<IReadOnlyList<ChatMessage>> GetMessagesAsync(Guid sessionId, CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
