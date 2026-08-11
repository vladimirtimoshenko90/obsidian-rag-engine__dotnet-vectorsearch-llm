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
        var existingNote = await noteRepo.GetByFilePath(noteFile.FilePath, ct);

        if (existingNote is not null)
        {
            if (existingNote.ContentHash == noteFile.ContentHash)
                return;

            await noteRepo.Delete(existingNote.Id, ct);
        }

        var sanitizedText = await noteSanitization.Sanitize(noteFile, ct);

        var note = await noteRepo.Create(new ObsidianNote
        {
            FilePath = noteFile.FilePath,
            ContentHash = noteFile.ContentHash,
            TextRaw = noteFile.Content,
            TextSanitized = sanitizedText
        }, ct);

        await vectorizationService.VectorizeNote(note, ct);
    }
}
