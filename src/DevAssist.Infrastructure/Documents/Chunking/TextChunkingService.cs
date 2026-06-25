using DevAssist.Application.Interfaces.Documents;

namespace DevAssist.Infrastructure.Documents.Chunking;

public sealed class TextChunkingService : ITextChunkingService
{
    public IReadOnlyList<TextChunkResult> Chunk(string text, int maxChunkSize = 1000, int overlap = 200)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return [];
        }

        var normalized = text.Replace("\r\n", "\n", StringComparison.Ordinal);
        var chunks = new List<TextChunkResult>();
        var start = 0;
        var order = 0;

        while (start < normalized.Length)
        {
            var length = Math.Min(maxChunkSize, normalized.Length - start);
            var chunkText = normalized.Substring(start, length).Trim();

            if (chunkText.Length > 0)
            {
                chunks.Add(new TextChunkResult(order++, chunkText));
            }

            if (start + length >= normalized.Length)
            {
                break;
            }

            start += Math.Max(1, maxChunkSize - overlap);
        }

        return chunks;
    }
}
