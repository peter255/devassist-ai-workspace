using DevAssist.Application.Interfaces;
using DevAssist.Infrastructure.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DevAssist.Infrastructure.Documents.Storage;

public sealed class LocalFileDocumentStorageService(
    IOptions<LocalFileStorageOptions> options,
    ILogger<LocalFileDocumentStorageService> logger) : IDocumentStorageService
{
    public async Task<string> UploadAsync(Stream stream, string fileName, string contentType, CancellationToken cancellationToken)
    {
        var root = Path.GetFullPath(options.Value.RootPath);
        Directory.CreateDirectory(root);

        var blobPath = $"{Guid.NewGuid():N}/{fileName}";
        var fullPath = Path.Combine(root, blobPath.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);

        await using var fileStream = File.Create(fullPath);
        if (stream.CanSeek)
        {
            stream.Position = 0;
        }

        await stream.CopyToAsync(fileStream, cancellationToken);
        logger.LogInformation("Stored document locally at {Path}.", fullPath);
        return blobPath;
    }

    public Task<Stream> OpenReadAsync(string blobPath, CancellationToken cancellationToken)
    {
        var root = Path.GetFullPath(options.Value.RootPath);
        var fullPath = Path.Combine(root, blobPath.Replace('/', Path.DirectorySeparatorChar));

        if (!File.Exists(fullPath))
        {
            throw new FileNotFoundException($"Document file not found at '{fullPath}'.");
        }

        Stream stream = File.OpenRead(fullPath);
        return Task.FromResult(stream);
    }
}
