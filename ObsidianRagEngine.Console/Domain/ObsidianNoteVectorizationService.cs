using ObsidianRagEngine.Console.Data.ObsidianNoteChunks.Entities;
using ObsidianRagEngine.Console.Data.ObsidianNoteChunks.Repositories;
using ObsidianRagEngine.Console.Data.ObsidianNotes.Entities;

namespace ObsidianRagEngine.Console.Domain;

public interface IObsidianNoteVectorizationService
{
    Task VectorizeNote(ObsidianNote note, CancellationToken ct = default);
}

public class ObsidianNoteVectorizationService(
    IObsidianNoteChunkRepository chunkRepo,
    ITextChunkingService chunkingService,
    IEmbeddingService embeddingService) : IObsidianNoteVectorizationService
{
    private const int ChunkSize = 700;
    private const int Overlap = 120;

    public async Task VectorizeNote(ObsidianNote note, CancellationToken ct = default)
    {
        var existingChunks = await chunkRepo.GetByNoteId(note.Id, ct);
        var existingByText = existingChunks.ToDictionary(c => c.Text);

        var newChunkTexts = await chunkingService.Split(note.TextSanitized, ChunkSize, Overlap);
        var newChunkTextSet = newChunkTexts.ToHashSet();

        var toDelete = existingChunks.Where(c => !newChunkTextSet.Contains(c.Text)).ToList();
        foreach (var stale in toDelete)
            await chunkRepo.Delete(stale.Id, ct);

        foreach (var chunkText in newChunkTexts)
        {
            if (existingByText.ContainsKey(chunkText))
                continue;

            var embedding = await embeddingService.EmbedAsync(chunkText, ct);

            await chunkRepo.Create(new ObsidianNoteChunk
            {
                Id = Guid.NewGuid(),
                NoteId = note.Id,
                Text = chunkText,
                Embedding = embedding
            }, ct);
        }
    }
}

// ---------------------------------------------------------------------------

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

// ---------------------------------------------------------------------------

public interface IEmbeddingService
{
    Task<float[]> EmbedAsync(string text, CancellationToken ct = default);
}