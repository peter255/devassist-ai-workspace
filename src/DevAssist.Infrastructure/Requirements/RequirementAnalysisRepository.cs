using DevAssist.Application.Interfaces.Requirements;
using DevAssist.Domain.Entities;
using DevAssist.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DevAssist.Infrastructure.Requirements;

public sealed class RequirementAnalysisRepository(DevAssistDbContext dbContext) : IRequirementAnalysisRepository
{
    public async Task AddAsync(RequirementAnalysis analysis, CancellationToken cancellationToken) =>
        await dbContext.RequirementAnalyses.AddAsync(analysis, cancellationToken);

    public async Task<RequirementAnalysis?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        await dbContext.RequirementAnalyses
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<IReadOnlyList<RequirementAnalysis>> GetRecentAsync(int limit, CancellationToken cancellationToken) =>
        await dbContext.RequirementAnalyses
            .AsNoTracking()
            .OrderByDescending(x => x.CreatedAt)
            .Take(limit)
            .ToListAsync(cancellationToken);

    public Task SaveChangesAsync(CancellationToken cancellationToken) =>
        dbContext.SaveChangesAsync(cancellationToken);
}
