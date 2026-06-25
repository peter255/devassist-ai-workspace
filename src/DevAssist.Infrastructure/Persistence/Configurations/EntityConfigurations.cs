using DevAssist.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DevAssist.Infrastructure.Persistence.Configurations;

public sealed class DocumentConfiguration : IEntityTypeConfiguration<Document>
{
    public void Configure(EntityTypeBuilder<Document> builder)
    {
        builder.ToTable("Documents");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.FileName).HasMaxLength(512).IsRequired();
        builder.Property(x => x.ContentType).HasMaxLength(100).IsRequired();
        builder.Property(x => x.BlobPath).HasMaxLength(1024).IsRequired();
        builder.Property(x => x.UploadedBy).HasMaxLength(150).IsRequired();
        builder.HasMany(x => x.Chunks).WithOne(x => x.Document).HasForeignKey(x => x.DocumentId);
    }
}

public sealed class DocumentChunkConfiguration : IEntityTypeConfiguration<DocumentChunk>
{
    public void Configure(EntityTypeBuilder<DocumentChunk> builder)
    {
        builder.ToTable("DocumentChunks");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.ChunkOrder).IsRequired();
        builder.Property(x => x.ChunkText).HasMaxLength(8000).IsRequired();
        builder.Property(x => x.SearchDocumentKey).HasMaxLength(256).IsRequired();
        builder.HasIndex(x => new { x.DocumentId, x.ChunkOrder }).IsUnique();
    }
}

public sealed class ChatSessionConfiguration : IEntityTypeConfiguration<ChatSession>
{
    public void Configure(EntityTypeBuilder<ChatSession> builder)
    {
        builder.ToTable("ChatSessions");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Title).HasMaxLength(200).IsRequired();
        builder.Property(x => x.CreatedBy).HasMaxLength(150).IsRequired();
    }
}

public sealed class ChatMessageConfiguration : IEntityTypeConfiguration<ChatMessage>
{
    public void Configure(EntityTypeBuilder<ChatMessage> builder)
    {
        builder.ToTable("ChatMessages");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Content).HasMaxLength(8000).IsRequired();
        builder.Property(x => x.CitationsJson).HasMaxLength(8000);
        builder.HasIndex(x => x.ChatSessionId);
        builder.HasOne(x => x.ChatSession).WithMany(x => x.Messages).HasForeignKey(x => x.ChatSessionId);
    }
}

public sealed class TicketAnalysisConfiguration : IEntityTypeConfiguration<TicketAnalysis>
{
    public void Configure(EntityTypeBuilder<TicketAnalysis> builder)
    {
        builder.ToTable("TicketAnalyses");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.OriginalText).HasMaxLength(12000).IsRequired();
        builder.Property(x => x.Summary).HasMaxLength(2500).IsRequired();
        builder.Property(x => x.Category).HasMaxLength(150).IsRequired();
        builder.Property(x => x.ImpactedModule).HasMaxLength(200).IsRequired();
        builder.Property(x => x.SuggestedAction).HasMaxLength(3000).IsRequired();
    }
}

public sealed class RequirementAnalysisConfiguration : IEntityTypeConfiguration<RequirementAnalysis>
{
    public void Configure(EntityTypeBuilder<RequirementAnalysis> builder)
    {
        builder.ToTable("RequirementAnalyses");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.OriginalText).HasMaxLength(12000).IsRequired();
        builder.Property(x => x.FunctionalSummary).HasMaxLength(3000).IsRequired();
        builder.Property(x => x.BackendTasksJson).HasMaxLength(8000).IsRequired();
        builder.Property(x => x.FrontendTasksJson).HasMaxLength(8000).IsRequired();
        builder.Property(x => x.TestingChecklistJson).HasMaxLength(8000).IsRequired();
        builder.Property(x => x.RisksJson).HasMaxLength(8000).IsRequired();
        builder.Property(x => x.AssumptionsJson).HasMaxLength(8000).IsRequired();
        builder.Property(x => x.AcceptanceCriteriaJson).HasMaxLength(8000).IsRequired();
    }
}
