using Azure.Storage.Blobs;
using DevAssist.Application.Interfaces;
using DevAssist.Infrastructure.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DevAssist.Infrastructure.Documents.Storage;

public sealed class AzureBlobDocumentStorageService(
    IOptions<BlobStorageOptions> options,
    ILogger<AzureBlobDocumentStorageService> logger) : IDocumentStorageService
{
    public async Task<string> UploadAsync(Stream stream, string fileName, string contentType, CancellationToken cancellationToken)
    {
        var settings = options.Value;
        if (string.IsNullOrWhiteSpace(settings.ConnectionString))
        {
            throw new InvalidOperationException("BlobStorage:ConnectionString is required for Azure blob uploads.");
        }

        var blobServiceClient = new BlobServiceClient(settings.ConnectionString);
        var containerClient = blobServiceClient.GetBlobContainerClient(settings.ContainerName);
        await containerClient.CreateIfNotExistsAsync(cancellationToken: cancellationToken);

        var blobPath = $"{Guid.NewGuid():N}/{fileName}";
        var blobClient = containerClient.GetBlobClient(blobPath);

        if (stream.CanSeek)
        {
            stream.Position = 0;
        }

        await blobClient.UploadAsync(stream, overwrite: true, cancellationToken);
        logger.LogInformation("Uploaded blob {BlobPath} to container {ContainerName}.", blobPath, settings.ContainerName);
        return blobPath;
    }

    public async Task<Stream> OpenReadAsync(string blobPath, CancellationToken cancellationToken)
    {
        var settings = options.Value;
        if (string.IsNullOrWhiteSpace(settings.ConnectionString))
        {
            throw new InvalidOperationException("BlobStorage:ConnectionString is required for Azure blob reads.");
        }

        var blobServiceClient = new BlobServiceClient(settings.ConnectionString);
        var containerClient = blobServiceClient.GetBlobContainerClient(settings.ContainerName);
        var blobClient = containerClient.GetBlobClient(blobPath);

        var response = await blobClient.DownloadStreamingAsync(cancellationToken: cancellationToken);
        return response.Value.Content;
    }
}
