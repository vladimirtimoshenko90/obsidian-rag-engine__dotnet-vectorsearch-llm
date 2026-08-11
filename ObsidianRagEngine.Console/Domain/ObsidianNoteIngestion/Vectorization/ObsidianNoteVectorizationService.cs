using ObsidianRagEngine.Console.Data.ObsidianNoteChunks.Entities;
using ObsidianRagEngine.Console.Data.ObsidianNoteChunks.Repositories;
using ObsidianRagEngine.Console.Data.ObsidianNotes.Entities;

namespace ObsidianRagEngine.Console.Domain.ObsidianNoteIngestion.Vectorization;

public interface IObsidianNoteVectorizationService
{
    Task VectorizeNote(ObsidianNote note, CancellationToken ct);
}

public class ObsidianNoteVectorizationService(
    IObsidianNoteChunkRepository chunkRepo,
    ITextChunkingService chunkingService,
    IEmbeddingService embeddingService) : IObsidianNoteVectorizationService
{
    private const int ChunkSize = 700;
    private const int Overlap = 120;

    public async Task VectorizeNote(ObsidianNote note, CancellationToken ct)
    {
        var existingChunks = await chunkRepo.GetByNoteId(note.Id, ct);
        var newChunkTexts = await chunkingService.Split(note.TextSanitized, ChunkSize, Overlap);
        var newChunkTextSet = newChunkTexts.ToHashSet();

        // A chunk is stale if its text is no longer needed.
        var toDelete = existingChunks
            .Where(c => !newChunkTextSet.Contains(c.Text))
            .ToList();
        foreach (var stale in toDelete)
            await chunkRepo.Delete(stale.Id, ct);

        // Only chunks that survived deletion are truly up-to-date.
        var upToDate = existingChunks
            .Except(toDelete)
            .Select(c => c.Text)
            .ToHashSet();

        foreach (var chunkText in newChunkTexts)
        {
            if (upToDate.Contains(chunkText))
                continue;

            var embedding = await embeddingService.Embed(chunkText, ct);

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
