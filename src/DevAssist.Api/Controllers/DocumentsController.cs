using DevAssist.Application.Documents.Commands.IndexDocument;
using DevAssist.Application.Documents.Commands.UploadDocument;
using DevAssist.Application.Documents.Queries.GetDocumentById;
using DevAssist.Application.Documents.Queries.GetDocuments;
using DevAssist.Contracts.Common;
using DevAssist.Contracts.Documents;
using DevAssist.Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace DevAssist.Api.Controllers;

[ApiController]
[Route("api/documents")]
public sealed class DocumentsController(
    IMediator mediator,
    IValidator<UploadDocumentCommand> uploadValidator,
    IValidator<IndexDocumentCommand> indexValidator) : ControllerBase
{
    [HttpPost("upload")]
    [RequestSizeLimit(10 * 1024 * 1024)]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<ApiResponse<UploadDocumentResponse>>> Upload(
        IFormFile file,
        [FromForm] DocumentType documentType,
        [FromForm] string uploadedBy = "system",
        CancellationToken cancellationToken = default)
    {
        if (file is null || file.Length == 0)
        {
            return BadRequest(ApiResponse<UploadDocumentResponse>.Fail("File is required."));
        }

        await using var stream = new MemoryStream();
        await file.CopyToAsync(stream, cancellationToken);
        stream.Position = 0;

        var command = new UploadDocumentCommand(
            stream,
            file.FileName,
            string.IsNullOrWhiteSpace(file.ContentType) ? "application/octet-stream" : file.ContentType,
            documentType,
            uploadedBy);

        var validation = await uploadValidator.ValidateAsync(command, cancellationToken);
        if (!validation.IsValid)
        {
            return BadRequest(ApiResponse<UploadDocumentResponse>.Fail(
                string.Join("; ", validation.Errors.Select(x => x.ErrorMessage))));
        }

        var result = await mediator.Send(command, cancellationToken);
        return Ok(ApiResponse<UploadDocumentResponse>.Ok(result));
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<DocumentSummaryDto>>>> List(CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetDocumentsQuery(), cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<DocumentSummaryDto>>.Ok(result));
    }

    [HttpGet("{documentId:guid}")]
    public async Task<ActionResult<ApiResponse<DocumentDetailsDto>>> GetById(Guid documentId, CancellationToken cancellationToken)
    {
        try
        {
            var result = await mediator.Send(new GetDocumentByIdQuery(documentId), cancellationToken);
            return Ok(ApiResponse<DocumentDetailsDto>.Ok(result));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResponse<DocumentDetailsDto>.Fail(ex.Message));
        }
    }

    [HttpPost("{documentId:guid}/index")]
    public async Task<ActionResult<ApiResponse<IndexDocumentResponse>>> Index(Guid documentId, CancellationToken cancellationToken)
    {
        var command = new IndexDocumentCommand(documentId);
        var validation = await indexValidator.ValidateAsync(command, cancellationToken);
        if (!validation.IsValid)
        {
            return BadRequest(ApiResponse<IndexDocumentResponse>.Fail(
                string.Join("; ", validation.Errors.Select(x => x.ErrorMessage))));
        }

        try
        {
            var result = await mediator.Send(command, cancellationToken);
            return Ok(ApiResponse<IndexDocumentResponse>.Ok(result));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResponse<IndexDocumentResponse>.Fail(ex.Message));
        }
    }
}
