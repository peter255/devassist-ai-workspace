namespace DevAssist.Application.Interfaces.Documents;

public sealed record TextChunkResult(int Order, string Text);

public interface ITextChunkingService
{
    IReadOnlyList<TextChunkResult> Chunk(string text, int maxChunkSize = 1000, int overlap = 200);
}
