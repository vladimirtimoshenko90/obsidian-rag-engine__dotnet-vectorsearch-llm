using ObsidianRagEngine.Console.Data.ObsidianNotes.Entities;
using ObsidianRagEngine.Console.Data.ObsidianNotes.Repositories;
using ObsidianRagEngine.Console.Domain.Ingestion.Sanitization;
using ObsidianRagEngine.Console.Domain.Reading;
using ObsidianRagEngine.Console.Domain.Vectorization;

namespace ObsidianRagEngine.Console.Domain.Ingestion;

public interface IObsidianNoteIngestionService
{
    Task ProcessNote(NoteFileData noteFile, CancellationToken ct = default);
}

public class ObsidianNoteIngestionService(
    IObsidianNoteRepository noteRepo,
    INoteSanitizationService noteSanitization,
    IObsidianNoteVectorizationService vectorizationService) : IObsidianNoteIngestionService
{
    public async Task ProcessNote(NoteFileData noteFile, CancellationToken ct = default)
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
