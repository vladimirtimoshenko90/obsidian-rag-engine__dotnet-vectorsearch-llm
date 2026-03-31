using ObsidianRagEngine.Console.Data.ObsidianNoteChunks.Entities;
using ObsidianRagEngine.Console.Data.ObsidianNoteChunks.Repositories;
using ObsidianRagEngine.Console.Data.ObsidianNotes.Entities;
using System.Net.Http.Json;

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
        var newChunkTexts = await chunkingService.Split(note.TextSanitized, ChunkSize, Overlap);
        var newChunkTextSet = newChunkTexts.ToHashSet();

        // A chunk is stale if its text is no longer needed or it was embedded with a different model.
        var toDelete = existingChunks
            .Where(c => !newChunkTextSet.Contains(c.Text) || c.EmbeddingModel != embeddingService.ModelName)
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
                Embedding = embedding,
                EmbeddingModel = embeddingService.ModelName
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
    string ModelName { get; }
    Task<float[]> Embed(string text, CancellationToken ct = default);
}

public class OllamaEmbeddingService(HttpClient httpClient, string modelName) : IEmbeddingService
{
    public string ModelName => modelName;

    public async Task<float[]> Embed(string text, CancellationToken ct = default)
    {
        var response = await httpClient.PostAsJsonAsync(
            "/api/embed",
            new OllamaEmbeddingRequest(modelName, text),
            ct);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<OllamaEmbeddingResponse>(ct);
        return result!.Embeddings.First();
    }

    private sealed record OllamaEmbeddingRequest(string Model, string Input);
    private sealed record OllamaEmbeddingResponse(float[][] Embeddings);
}