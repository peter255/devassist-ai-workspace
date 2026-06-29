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
            // Step 1 — open file from storage.
            logger.LogInformation("[Step 1/5] Opening file '{BlobPath}' from storage for document {DocumentId}.",
                document.BlobPath, document.Id);
            await using var stream = await documentStorageService.OpenReadAsync(document.BlobPath, cancellationToken);

            // Step 2 — extract text.
            logger.LogInformation("[Step 2/5] Extracting text from '{FileName}' (type: {ContentType}).",
                document.FileName, document.ContentType);
            var text = await textExtractionService.ExtractAsync(
                stream,
                document.FileName,
                document.ContentType,
                cancellationToken);
            logger.LogInformation("[Step 2/5] Extracted {Chars} characters.", text.Length);

            // Step 3 — chunk text.
            logger.LogInformation("[Step 3/5] Chunking extracted text.");
            var chunks = textChunkingService.Chunk(text);
            if (chunks.Count == 0)
                throw new InvalidOperationException("No extractable text content was found in the document.");
            logger.LogInformation("[Step 3/5] Produced {ChunkCount} chunks.", chunks.Count);

            // Step 4 — generate embeddings (failures fall back to empty vectors; indexing continues).
            logger.LogInformation("[Step 4/5] Generating embeddings for {ChunkCount} chunks.", chunks.Count);
            var embeddings = await embeddingService.GenerateEmbeddingsAsync(
                chunks.Select(x => x.Text).ToList(),
                cancellationToken);
            logger.LogInformation("[Step 4/5] Received {EmbeddingCount} embedding vectors.", embeddings.Count);

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

            // Step 5 — upsert to Azure Search (non-fatal) and persist chunks to SQL.
            logger.LogInformation("[Step 5/5] Upserting to Azure Search and saving {ChunkCount} chunks to SQL.", documentChunks.Count);
            await searchIndexer.UpsertChunksAsync(searchDocuments, cancellationToken);
            await documentRepository.ReplaceChunksAsync(document.Id, documentChunks, cancellationToken);

            document.Status = DocumentStatus.Indexed;
            await documentRepository.SaveChangesAsync(cancellationToken);

            logger.LogInformation(
                "Successfully indexed document {DocumentId} ('{FileName}') with {ChunkCount} chunks.",
                document.Id, document.FileName, documentChunks.Count);
            return new DocumentIndexingResult(document.Status.ToString(), documentChunks.Count);
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "Indexing FAILED at document {DocumentId} ('{FileName}'). " +
                "Error type: {ErrorType}. Message: {ErrorMessage}",
                document.Id, document.FileName, ex.GetType().Name, ex.Message);
            document.Status = DocumentStatus.Failed;
            await documentRepository.SaveChangesAsync(cancellationToken);
            return new DocumentIndexingResult(document.Status.ToString(), 0);
        }
    }
}
