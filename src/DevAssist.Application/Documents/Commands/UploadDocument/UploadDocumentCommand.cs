using DevAssist.Contracts.Documents;
using DevAssist.Domain.Enums;
using MediatR;

namespace DevAssist.Application.Documents.Commands.UploadDocument;

public sealed record UploadDocumentCommand(
    Stream FileStream,
    string FileName,
    string ContentType,
    DocumentType DocumentType,
    string UploadedBy) : IRequest<UploadDocumentResponse>;
