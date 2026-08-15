using ObsidianRagEngine.Console.Data.ObsidianNoteChunks.Repositories;
using ObsidianRagEngine.Console.Data.ObsidianNotes.Entities;
using ObsidianRagEngine.Console.Data.ObsidianNotes.Repositories;
using ObsidianRagEngine.Console.Domain.ObsidianNoteIngestion.Sanitization;
using ObsidianRagEngine.Console.Domain.ObsidianNoteIngestion.Vectorization;
using ObsidianRagEngine.Console.Domain.ObsidianVault;

namespace ObsidianRagEngine.Console.Domain.ObsidianNoteIngestion;

public interface IObsidianNoteIngestionService
{
    Task IngestNote(NoteFileData noteFile, CancellationToken ct);
}

public class ObsidianNoteIngestionService(
    IObsidianNoteRepository noteRepo,
    IObsidianNoteChunkRepository chunkRepo,
    IObsidianNoteSanitizationService noteSanitization,
    IObsidianNoteVectorizationService vectorizationService) : IObsidianNoteIngestionService
{
    public async Task IngestNote(NoteFileData noteFile, CancellationToken ct)
    {
        var note = await noteRepo.GetByFilePath(noteFile.FilePath, ct);

        if (note is null || note.ContentHash != noteFile.ContentHash)
        {
            if (note is not null)
            {
                await chunkRepo.DeleteByNoteId(note.Id, ct);
                await noteRepo.Delete(note.Id, ct);
            }

            var (sanitizedContent, cost) = await noteSanitization.Sanitize(noteFile, ct);

            note = await noteRepo.Create(new ObsidianNote
            {
                FilePath = noteFile.FilePath,
                ContentHash = noteFile.ContentHash,
                TextRaw = noteFile.Content,
                TextSanitized = sanitizedContent,
                Cost = cost,
            }, ct);
        }

        await vectorizationService.VectorizeNote(note, ct);
    }
}
