using ObsidianRagEngine.Console.Data.ObsidianNotes.Entities;
using ObsidianRagEngine.Console.Data.ObsidianNotes.Repositories;
using ObsidianRagEngine.Console.Domain.ObsidianNoteIngestion.Sanitization;
using ObsidianRagEngine.Console.Domain.ObsidianNoteIngestion.Vectorization;
using ObsidianRagEngine.Console.Domain.Reading;

namespace ObsidianRagEngine.Console.Domain.ObsidianNoteIngestion;

public interface IObsidianNoteIngestionService
{
    Task IngestNote(NoteFileData noteFile, CancellationToken ct);
}

public class ObsidianNoteIngestionService(
    IObsidianNoteRepository noteRepo,
    IObsidianNoteSanitizationService noteSanitization,
    IObsidianNoteVectorizationService vectorizationService) : IObsidianNoteIngestionService
{
    public async Task IngestNote(NoteFileData noteFile, CancellationToken ct)
    {
        var note = await noteRepo.GetByFilePath(noteFile.FilePath, ct);

        if (note is null || note.ContentHash != noteFile.ContentHash)
        {
            if (note is not null)
                await noteRepo.Delete(note.Id, ct);

            string sanitizedContent = await noteSanitization.Sanitize(noteFile, ct);

            note = await noteRepo.Create(new ObsidianNote
            {
                FilePath = noteFile.FilePath,
                ContentHash = noteFile.ContentHash,
                TextRaw = noteFile.Content,
                TextSanitized = sanitizedContent
            }, ct);
        }

        await vectorizationService.VectorizeNote(note, ct);
    }
}
