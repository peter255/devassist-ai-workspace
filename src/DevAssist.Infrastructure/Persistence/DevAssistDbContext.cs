using DevAssist.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace DevAssist.Infrastructure.Persistence;

public sealed class DevAssistDbContext(DbContextOptions<DevAssistDbContext> options) : DbContext(options)
{
    public DbSet<Document> Documents => Set<Document>();
    public DbSet<DocumentChunk> DocumentChunks => Set<DocumentChunk>();
    public DbSet<ChatSession> ChatSessions => Set<ChatSession>();
    public DbSet<ChatMessage> ChatMessages => Set<ChatMessage>();
    public DbSet<TicketAnalysis> TicketAnalyses => Set<TicketAnalysis>();
    public DbSet<RequirementAnalysis> RequirementAnalyses => Set<RequirementAnalysis>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(DevAssistDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
