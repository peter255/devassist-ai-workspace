using DevAssist.Domain.Enums;

namespace DevAssist.Domain.Entities;

public sealed class Document
{
    public Guid Id { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public string BlobPath { get; set; } = string.Empty;
    public DateTimeOffset UploadedAt { get; set; }
    public string UploadedBy { get; set; } = string.Empty;
    public DocumentStatus Status { get; set; }
    public DocumentType DocumentType { get; set; }
    public ICollection<DocumentChunk> Chunks { get; set; } = new List<DocumentChunk>();
}

public sealed class DocumentChunk
{
    public Guid Id { get; set; }
    public Guid DocumentId { get; set; }
    public int ChunkOrder { get; set; }
    public string ChunkText { get; set; } = string.Empty;
    public string SearchDocumentKey { get; set; } = string.Empty;
    public Document? Document { get; set; }
}

public sealed class ChatSession
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public ICollection<ChatMessage> Messages { get; set; } = new List<ChatMessage>();
}

public sealed class ChatMessage
{
    public Guid Id { get; set; }
    public Guid ChatSessionId { get; set; }
    public ChatMessageRole Role { get; set; }
    public string Content { get; set; } = string.Empty;
    public string? CitationsJson { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public ChatSession? ChatSession { get; set; }
}

public sealed class TicketAnalysis
{
    public Guid Id { get; set; }
    public string OriginalText { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public TicketSeverity Severity { get; set; }
    public string Category { get; set; } = string.Empty;
    public string ImpactedModule { get; set; } = string.Empty;
    public string SuggestedAction { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
}

public sealed class RequirementAnalysis
{
    public Guid Id { get; set; }
    public string OriginalText { get; set; } = string.Empty;
    public string FunctionalSummary { get; set; } = string.Empty;
    public string BackendTasksJson { get; set; } = "[]";
    public string FrontendTasksJson { get; set; } = "[]";
    public string TestingChecklistJson { get; set; } = "[]";
    public string RisksJson { get; set; } = "[]";
    public string AssumptionsJson { get; set; } = "[]";
    public string AcceptanceCriteriaJson { get; set; } = "[]";
    public DateTimeOffset CreatedAt { get; set; }
}
