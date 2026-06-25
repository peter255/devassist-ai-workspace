using DevAssist.Application.Interfaces.Tickets;
using DevAssist.Domain.Entities;
using DevAssist.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DevAssist.Infrastructure.Tickets;

public sealed class TicketAnalysisRepository(DevAssistDbContext dbContext) : ITicketAnalysisRepository
{
    public async Task AddAsync(TicketAnalysis analysis, CancellationToken cancellationToken) =>
        await dbContext.TicketAnalyses.AddAsync(analysis, cancellationToken);

    public async Task<IReadOnlyList<TicketAnalysis>> GetRecentAsync(int limit, CancellationToken cancellationToken) =>
        await dbContext.TicketAnalyses
            .AsNoTracking()
            .OrderByDescending(x => x.CreatedAt)
            .Take(limit)
            .ToListAsync(cancellationToken);

    public Task SaveChangesAsync(CancellationToken cancellationToken) =>
        dbContext.SaveChangesAsync(cancellationToken);
}
