using DevAssist.Application.Interfaces.Documents;
using System.Text;
using UglyToad.PdfPig;

namespace DevAssist.Infrastructure.Documents.Extraction;

public sealed class PlainTextDocumentExtractor : IDocumentTextExtractor
{
    private static readonly HashSet<string> SupportedExtensions = [".txt", ".md"];

    public bool CanExtract(string fileName, string contentType) =>
        SupportedExtensions.Contains(Path.GetExtension(fileName).ToLowerInvariant())
        || contentType.StartsWith("text/", StringComparison.OrdinalIgnoreCase);

    public async Task<string> ExtractAsync(Stream stream, string fileName, string contentType, CancellationToken cancellationToken)
    {
        using var reader = new StreamReader(stream, leaveOpen: true);
        return await reader.ReadToEndAsync(cancellationToken);
    }
}

/// <summary>
/// Extracts text from PDF files using PdfPig.
/// Uses page.Letters (always available) to build full-text per page.
/// </summary>
public sealed class PdfDocumentExtractor : IDocumentTextExtractor
{
    public bool CanExtract(string fileName, string contentType) =>
        Path.GetExtension(fileName).Equals(".pdf", StringComparison.OrdinalIgnoreCase)
        || contentType.Equals("application/pdf", StringComparison.OrdinalIgnoreCase);

    public Task<string> ExtractAsync(Stream stream, string fileName, string contentType, CancellationToken cancellationToken)
    {
        using var ms = new MemoryStream();
        stream.CopyTo(ms);
        var bytes = ms.ToArray();

        if (bytes.Length == 0)
            throw new InvalidOperationException("The PDF file is empty.");

        var sb = new StringBuilder();

        using var pdf = PdfDocument.Open(bytes);
        foreach (var page in pdf.GetPages())
        {
            // Letters is always available — concatenate character values directly.
            var pageText = string.Concat(page.Letters.Select(l => l.Value));
            if (!string.IsNullOrWhiteSpace(pageText))
            {
                sb.AppendLine(pageText.Trim());
                sb.AppendLine();
            }
        }

        var result = sb.ToString().Trim();
        if (string.IsNullOrWhiteSpace(result))
            throw new InvalidOperationException(
                "No text could be extracted from this PDF. " +
                "It may be a scanned image PDF without an embedded text layer.");

        return Task.FromResult(result);
    }
}

public sealed class UnsupportedDocumentExtractor : IDocumentTextExtractor
{
    public bool CanExtract(string fileName, string contentType) => false;

    public Task<string> ExtractAsync(Stream stream, string fileName, string contentType, CancellationToken cancellationToken)
    {
        var extension = Path.GetExtension(fileName);
        throw new NotSupportedException(
            $"Text extraction for '{extension}' is not supported. Supported formats: .txt, .md, .pdf.");
    }
}

public sealed class DocumentTextExtractionService(IEnumerable<IDocumentTextExtractor> extractors)
{
    public Task<string> ExtractAsync(Stream stream, string fileName, string contentType, CancellationToken cancellationToken)
    {
        var extractor = extractors.FirstOrDefault(x => x.CanExtract(fileName, contentType))
            ?? extractors.OfType<UnsupportedDocumentExtractor>().First();

        if (stream.CanSeek)
            stream.Position = 0;

        return extractor.ExtractAsync(stream, fileName, contentType, cancellationToken);
    }
}
