using DevAssist.Application.Interfaces.Copilot;
using DevAssist.Domain.Enums;
using DevAssist.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DevAssist.Infrastructure.Copilot.Search;

/// <summary>
/// Local retrieval fallback: keyword search over indexed SQL chunks when Azure AI Search is not configured.
/// </summary>
public sealed class SqlDocumentSearchRetriever(
    DevAssistDbContext dbContext,
    ILogger<SqlDocumentSearchRetriever> logger) : IDocumentSearchRetriever
{
    public async Task<IReadOnlyList<RetrievedChunk>> SearchAsync(
        string question,
        int top = 5,
        CancellationToken cancellationToken = default)
    {
        var terms = question
            .Split([' ', '\n', '\r', '\t'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(t => t.Length > 2)
            .Select(t => t.ToLowerInvariant())
            .Distinct()
            .ToList();

        if (terms.Count == 0)
        {
            terms = [question.ToLowerInvariant()];
        }

        var allChunks = await dbContext.DocumentChunks
            .AsNoTracking()
            .Include(x => x.Document)
            .Where(x => x.Document != null && x.Document.Status == DocumentStatus.Indexed)
            .ToListAsync(cancellationToken);

        // When every search term is non-Latin script (Arabic, Hebrew, CJK, etc.) the terms
        // will never match English document content. In that case return the first chunk from
        // each indexed document as "broad context" so the LLM always has something to work
        // with and can decide whether any of it is relevant to the question.
        var allNonLatin = terms.All(t => t.All(c => c > '\u024F'));
        if (allNonLatin)
        {
            var broadContext = allChunks
                .GroupBy(c => c.DocumentId)
                .Select(g => g.OrderBy(c => c.ChunkOrder).First())
                .Take(top)
                .Select(c => new RetrievedChunk(
                    c.DocumentId,
                    c.Document?.FileName ?? "unknown",
                    c.Document?.DocumentType.ToString() ?? "Other",
                    c.SearchDocumentKey,
                    c.ChunkText,
                    0.5))
                .ToList();

            logger.LogInformation(
                "Non-Latin query: returning {Count} broad-context chunks from {DocCount} document(s).",
                broadContext.Count,
                broadContext.Select(x => x.DocumentName).Distinct().Count());
            return broadContext;
        }

        var scored = allChunks
            .Select(chunk =>
            {
                var text = chunk.ChunkText.ToLowerInvariant();
                var score = terms.Sum(term => text.Contains(term, StringComparison.Ordinal) ? 1.0 : 0.0);
                return new { chunk, score };
            })
            .Where(x => x.score > 0)
            .OrderByDescending(x => x.score)
            .ThenBy(x => x.chunk.ChunkOrder)
            .Take(top)
            .Select(x => new RetrievedChunk(
                x.chunk.DocumentId,
                x.chunk.Document?.FileName ?? "unknown",
                x.chunk.Document?.DocumentType.ToString() ?? "Other",
                x.chunk.SearchDocumentKey,
                x.chunk.ChunkText,
                x.score))
            .ToList();

        logger.LogInformation("SQL retrieval returned {Count} chunks for question.", scored.Count);
        return scored;
    }
}
