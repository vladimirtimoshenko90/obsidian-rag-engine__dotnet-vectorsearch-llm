using Microsoft.EntityFrameworkCore;
using ObsidianRagEngine.Console.Data.Entities;

namespace ObsidianRagEngine.Console.Data.Repositories;

public interface IObsidianNoteRepository
{
    Task<ObsidianNote> Create(ObsidianNote note, CancellationToken ct = default);
    Task Delete(int id, CancellationToken ct = default);
    Task<ObsidianNote?> GetByFilePath(string filePath, CancellationToken ct = default);
}

public class ObsidianNoteRepository(ObsidianNotesDbContext db) : IObsidianNoteRepository
{
    public async Task<ObsidianNote> Create(ObsidianNote note, CancellationToken ct = default)
    {
        db.ObsidianNotes.Add(note);
        await db.SaveChangesAsync(ct);
        return note;
    }

    public async Task Delete(int id, CancellationToken ct = default)
    {
        var note = await db.ObsidianNotes.FindAsync([id], ct);
        if (note is null)
            return;

        db.ObsidianNotes.Remove(note);
        await db.SaveChangesAsync(ct);
    }

    public Task<ObsidianNote?> GetByFilePath(string filePath, CancellationToken ct = default)
    {
        return db.ObsidianNotes
            .FirstOrDefaultAsync(n => n.FilePath == filePath, ct);
    }
}
