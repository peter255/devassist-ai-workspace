using DevAssist.Application.Interfaces;
using DevAssist.Application.Interfaces.Documents;
using DevAssist.Domain.Entities;
using DevAssist.Domain.Enums;
using DevAssist.Infrastructure.Documents.Extraction;
using Microsoft.Extensions.Logging;

namespace DevAssist.Infrastructure.Documents.Indexing;

public sealed class DocumentIndexingOrchestrator(
    IDocumentRepository documentRepository,
    IDocumentStorageService documentStorageService,
    DocumentTextExtractionService textExtractionService,
    ITextChunkingService textChunkingService,
    IEmbeddingService embeddingService,
    IDocumentSearchIndexer searchIndexer,
    ILogger<DocumentIndexingOrchestrator> logger) : IDocumentIndexingOrchestrator
{
    public async Task<DocumentIndexingResult> IndexAsync(Guid documentId, CancellationToken cancellationToken)
    {
        var document = await documentRepository.GetByIdAsync(documentId, cancellationToken)
            ?? throw new KeyNotFoundException($"Document '{documentId}' was not found.");

        document.Status = DocumentStatus.Processing;
        await documentRepository.SaveChangesAsync(cancellationToken);

        try
        {
            await using var stream = await documentStorageService.OpenReadAsync(document.BlobPath, cancellationToken);
            var text = await textExtractionService.ExtractAsync(
                stream,
                document.FileName,
                document.ContentType,
                cancellationToken);

            var chunks = textChunkingService.Chunk(text);
            if (chunks.Count == 0)
            {
                throw new InvalidOperationException("No extractable text content was found in the document.");
            }

            var embeddings = await embeddingService.GenerateEmbeddingsAsync(
                chunks.Select(x => x.Text).ToList(),
                cancellationToken);

            var documentChunks = new List<DocumentChunk>();
            var searchDocuments = new List<SearchChunkDocument>();

            for (var i = 0; i < chunks.Count; i++)
            {
                var chunk = chunks[i];
                var searchKey = $"{document.Id:N}-{chunk.Order}";
                documentChunks.Add(new DocumentChunk
                {
                    Id = Guid.NewGuid(),
                    DocumentId = document.Id,
                    ChunkOrder = chunk.Order,
                    ChunkText = chunk.Text,
                    SearchDocumentKey = searchKey
                });

                searchDocuments.Add(new SearchChunkDocument(
                    searchKey,
                    document.Id,
                    document.FileName,
                    document.DocumentType.ToString(),
                    chunk.Order,
                    chunk.Text,
                    i < embeddings.Count ? embeddings[i] : null));
            }

            await searchIndexer.UpsertChunksAsync(searchDocuments, cancellationToken);
            await documentRepository.ReplaceChunksAsync(document.Id, documentChunks, cancellationToken);

            document.Status = DocumentStatus.Indexed;
            await documentRepository.SaveChangesAsync(cancellationToken);

            logger.LogInformation("Indexed document {DocumentId} with {ChunkCount} chunks.", document.Id, documentChunks.Count);
            return new DocumentIndexingResult(document.Status.ToString(), documentChunks.Count);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to index document {DocumentId}.", document.Id);
            document.Status = DocumentStatus.Failed;
            await documentRepository.SaveChangesAsync(cancellationToken);
            return new DocumentIndexingResult(document.Status.ToString(), 0);
        }
    }
}
