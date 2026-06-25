using DevAssist.Application.Interfaces.Documents;

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

public sealed class UnsupportedDocumentExtractor : IDocumentTextExtractor
{
    public bool CanExtract(string fileName, string contentType) => false;

    public Task<string> ExtractAsync(Stream stream, string fileName, string contentType, CancellationToken cancellationToken)
    {
        var extension = Path.GetExtension(fileName);
        throw new NotSupportedException(
            $"Text extraction for '{extension}' is not implemented yet. Supported formats: .txt, .md.");
    }
}

public sealed class DocumentTextExtractionService(IEnumerable<IDocumentTextExtractor> extractors)
{
    public Task<string> ExtractAsync(Stream stream, string fileName, string contentType, CancellationToken cancellationToken)
    {
        var extractor = extractors.FirstOrDefault(x => x.CanExtract(fileName, contentType))
            ?? extractors.OfType<UnsupportedDocumentExtractor>().First();

        if (stream.CanSeek)
        {
            stream.Position = 0;
        }

        return extractor.ExtractAsync(stream, fileName, contentType, cancellationToken);
    }
}
