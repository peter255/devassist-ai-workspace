using DevAssist.Contracts.Documents;
using DevAssist.Domain.Entities;

namespace DevAssist.Application.Documents.Mappings;

public static class DocumentMapper
{
    public static DocumentSummaryDto ToSummary(Document document) =>
        new(
            document.Id,
            document.FileName,
            document.ContentType,
            document.Status.ToString(),
            document.DocumentType.ToString(),
            document.UploadedAt,
            document.UploadedBy);

    public static DocumentDetailsDto ToDetails(Document document) =>
        new(
            document.Id,
            document.FileName,
            document.ContentType,
            document.BlobPath,
            document.Status.ToString(),
            document.DocumentType.ToString(),
            document.UploadedAt,
            document.UploadedBy,
            document.Chunks.Count);
}
