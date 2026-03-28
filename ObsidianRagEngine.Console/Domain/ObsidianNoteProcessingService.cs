using ObsidianRagEngine.Console.Data.ObsidianNotes.Entities;
using ObsidianRagEngine.Console.Data.ObsidianNotes.Repositories;

namespace ObsidianRagEngine.Console.Domain;

public interface IObsidianNoteProcessingService
{
    Task ProcessNote(ObsidianNoteInfo noteInfo, CancellationToken ct = default);
}

public class ObsidianNoteProcessingService(
    IObsidianRepositoryReader obsidianRepo,
    IObsidianNoteRepository noteRepo,
    IObsidianImageRepository noteImageRepo) : IObsidianNoteProcessingService
{
    public async Task ProcessNote(ObsidianNoteInfo noteInfo, CancellationToken ct = default)
    {
        var noteFile = await obsidianRepo.ReadNote(noteInfo.FilePath);

        var existingNote = await noteRepo.GetByFilePath(noteInfo.FilePath, ct);

        if (existingNote is not null)
        {
            if (existingNote.ContentHash == noteFile.ContentHash)
                return;

            await noteImageRepo.DeleteByNoteId(existingNote.Id, ct);
            await noteRepo.Delete(existingNote.Id, ct);
        }

        var newNote = await noteRepo.Create(new ObsidianNote
        {
            FilePath = noteFile.FilePath,
            ContentHash = noteFile.ContentHash,
            Text = noteFile.Content
        }, ct);

        foreach (var imageName in noteFile.Images)
        {
            await noteImageRepo.Create(new ObsidianImage
            {
                NoteId = newNote.Id,
                FilePath = imageName
            }, ct);
        }
    }
}