namespace DevAssist.Application.Interfaces.Documents;

public interface IDocumentTextExtractor
{
    bool CanExtract(string fileName, string contentType);
    Task<string> ExtractAsync(Stream stream, string fileName, string contentType, CancellationToken cancellationToken);
}
