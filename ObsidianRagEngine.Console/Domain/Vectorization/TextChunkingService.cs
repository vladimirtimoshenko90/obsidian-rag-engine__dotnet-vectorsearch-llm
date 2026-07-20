namespace ObsidianRagEngine.Console.Domain.Vectorization;

public interface ITextChunkingService
{
    Task<List<string>> Split(string text, int chunkSize, int overlap);
}

public class TextChunkingService : ITextChunkingService
{
    public Task<List<string>> Split(string text, int chunkSize, int overlap)
    {
        if (text.Length <= chunkSize)
            return Task.FromResult(new List<string> { text });

        var chunks = new List<string>();
        var i = 0;

        while (i < text.Length)
        {
            var length = Math.Min(chunkSize, text.Length - i);
            chunks.Add(text.Substring(i, length));
            i += chunkSize - overlap;
        }

        return Task.FromResult(chunks);
    }
}
