using DevAssist.Application.Interfaces;
using DevAssist.Application.Interfaces.Documents;
using DevAssist.Contracts.Documents;
using DevAssist.Domain.Entities;
using DevAssist.Domain.Enums;
using FluentValidation;
using MediatR;

namespace DevAssist.Application.Documents.Commands.UploadDocument;

public sealed class UploadDocumentCommandValidator : AbstractValidator<UploadDocumentCommand>
{
    private static readonly HashSet<string> AllowedExtensions = [".txt", ".md", ".pdf", ".docx"];
    private const long MaxFileSizeBytes = 10 * 1024 * 1024;

    public UploadDocumentCommandValidator()
    {
        RuleFor(x => x.FileStream).NotNull();
        RuleFor(x => x.FileName).NotEmpty().MaximumLength(512);
        RuleFor(x => x.ContentType).NotEmpty().MaximumLength(100);
        RuleFor(x => x.UploadedBy).NotEmpty().MaximumLength(150);
        RuleFor(x => x.DocumentType).IsInEnum();
        RuleFor(x => x.FileName)
            .Must(fileName => AllowedExtensions.Contains(Path.GetExtension(fileName).ToLowerInvariant()))
            .WithMessage("Supported file types: .txt, .md, .pdf, .docx");
        RuleFor(x => x)
            .Must(command => command.FileStream.CanSeek && command.FileStream.Length <= MaxFileSizeBytes)
            .WithMessage($"File size must not exceed {MaxFileSizeBytes / (1024 * 1024)} MB.");
    }
}

public sealed class UploadDocumentCommandHandler(
    IDocumentStorageService documentStorageService,
    IDocumentRepository documentRepository) : IRequestHandler<UploadDocumentCommand, UploadDocumentResponse>
{
    public async Task<UploadDocumentResponse> Handle(UploadDocumentCommand request, CancellationToken cancellationToken)
    {
        var blobPath = await documentStorageService.UploadAsync(
            request.FileStream,
            request.FileName,
            request.ContentType,
            cancellationToken);

        var document = new Document
        {
            Id = Guid.NewGuid(),
            FileName = request.FileName,
            ContentType = request.ContentType,
            BlobPath = blobPath,
            UploadedAt = DateTimeOffset.UtcNow,
            UploadedBy = request.UploadedBy,
            Status = DocumentStatus.Uploaded,
            DocumentType = request.DocumentType
        };

        await documentRepository.AddAsync(document, cancellationToken);
        await documentRepository.SaveChangesAsync(cancellationToken);

        return new UploadDocumentResponse(document.Id, document.FileName, document.Status.ToString());
    }
}
