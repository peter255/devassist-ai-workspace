using Azure;
using Azure.AI.DocumentIntelligence;
using DevAssist.Application.Interfaces.Documents;
using DevAssist.Infrastructure.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DevAssist.Infrastructure.Documents.Extraction;

/// <summary>
/// Extracts text from PDF files (including scanned/image PDFs) using Azure Document Intelligence
/// prebuilt-read model. Registered only when DocumentIntelligence:Endpoint and ApiKey are set.
/// Replaces PdfDocumentExtractor as the PDF handler when configured.
/// </summary>
public sealed class AzureDocumentIntelligenceOcrExtractor(
    IOptions<DocumentIntelligenceOptions> options,
    ILogger<AzureDocumentIntelligenceOcrExtractor> logger) : IDocumentTextExtractor
{
    public bool CanExtract(string fileName, string contentType) =>
        Path.GetExtension(fileName).Equals(".pdf", StringComparison.OrdinalIgnoreCase)
        || contentType.Equals("application/pdf", StringComparison.OrdinalIgnoreCase);

    public async Task<string> ExtractAsync(
        Stream stream,
        string fileName,
        string contentType,
        CancellationToken cancellationToken)
    {
        var settings = options.Value;
        if (string.IsNullOrWhiteSpace(settings.Endpoint) || string.IsNullOrWhiteSpace(settings.ApiKey))
            throw new InvalidOperationException("Azure Document Intelligence is not configured.");

        using var ms = new MemoryStream();
        await stream.CopyToAsync(ms, cancellationToken);
        ms.Position = 0;

        var client = new DocumentIntelligenceClient(
            new Uri(settings.Endpoint),
            new AzureKeyCredential(settings.ApiKey));

        logger.LogInformation("Extracting text from PDF '{FileName}' via Azure Document Intelligence.", fileName);

        // SDK 1.0.0 GA: pass BinaryData directly (AnalyzeDocumentContent was removed in GA).
        var binaryData = BinaryData.FromBytes(ms.ToArray());
        var operation = await client.AnalyzeDocumentAsync(
            WaitUntil.Completed,
            "prebuilt-read",
            binaryData,
            cancellationToken: cancellationToken);

        var result = operation.Value;
        var text = result.Content;

        if (string.IsNullOrWhiteSpace(text))
            throw new InvalidOperationException(
                "Azure Document Intelligence could not extract any text from this PDF.");

        logger.LogInformation(
            "Document Intelligence extracted {Length} characters from '{FileName}'.",
            text.Length, fileName);

        return text;
    }
}
