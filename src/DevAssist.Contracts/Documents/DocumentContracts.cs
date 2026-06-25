namespace DevAssist.Contracts.Documents;

public sealed record DocumentSummaryDto(
    Guid Id,
    string FileName,
    string ContentType,
    string Status,
    string DocumentType,
    DateTimeOffset UploadedAt,
    string UploadedBy);

public sealed record UploadDocumentResponse(
    Guid Id,
    string FileName,
    string Status);

public sealed record DocumentDetailsDto(
    Guid Id,
    string FileName,
    string ContentType,
    string BlobPath,
    string Status,
    string DocumentType,
    DateTimeOffset UploadedAt,
    string UploadedBy,
    int ChunkCount);

public sealed record IndexDocumentResponse(
    Guid Id,
    string Status,
    int ChunkCount);
