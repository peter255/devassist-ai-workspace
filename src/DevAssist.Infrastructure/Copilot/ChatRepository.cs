using DevAssist.Application.Interfaces.Copilot;
using DevAssist.Domain.Entities;
using DevAssist.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DevAssist.Infrastructure.Copilot;

public sealed class ChatRepository(DevAssistDbContext dbContext) : IChatRepository
{
    public Task<ChatSession?> GetSessionByIdAsync(Guid sessionId, CancellationToken cancellationToken) =>
        dbContext.ChatSessions.FirstOrDefaultAsync(x => x.Id == sessionId, cancellationToken);

    public async Task<ChatSession> CreateSessionAsync(ChatSession session, CancellationToken cancellationToken)
    {
        await dbContext.ChatSessions.AddAsync(session, cancellationToken);
        return session;
    }

    public async Task AddMessageAsync(ChatMessage message, CancellationToken cancellationToken) =>
        await dbContext.ChatMessages.AddAsync(message, cancellationToken);

    public async Task<IReadOnlyList<ChatMessage>> GetMessagesAsync(Guid sessionId, CancellationToken cancellationToken) =>
        await dbContext.ChatMessages
            .AsNoTracking()
            .Where(x => x.ChatSessionId == sessionId)
            .OrderBy(x => x.CreatedAt)
            .ToListAsync(cancellationToken);

    public Task SaveChangesAsync(CancellationToken cancellationToken) =>
        dbContext.SaveChangesAsync(cancellationToken);
}
