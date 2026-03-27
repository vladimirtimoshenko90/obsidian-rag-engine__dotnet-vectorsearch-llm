using Microsoft.EntityFrameworkCore;
using ObsidianRagEngine.Console.Data.ObsidianNotes.Entities;

namespace ObsidianRagEngine.Console.Data.ObsidianNotes.Repositories;

public interface IObsidianImageRepository
{
    Task<ObsidianImage> Create(ObsidianImage image, CancellationToken ct = default);
    Task Delete(int id, CancellationToken ct = default);
    Task<ObsidianImage?> GetByFilePath(string filePath, CancellationToken ct = default);
}

public class ObsidianImageRepository(ObsidianNotesDbContext db) : IObsidianImageRepository
{
    public async Task<ObsidianImage> Create(ObsidianImage image, CancellationToken ct = default)
    {
        db.ObsidianImages.Add(image);
        await db.SaveChangesAsync(ct);
        return image;
    }

    public async Task Delete(int id, CancellationToken ct = default)
    {
        var image = await db.ObsidianImages.FindAsync([id], ct);
        if (image is null)
            return;

        db.ObsidianImages.Remove(image);
        await db.SaveChangesAsync(ct);
    }

    public Task<ObsidianImage?> GetByFilePath(string filePath, CancellationToken ct = default)
    {
        return db.ObsidianImages
            .FirstOrDefaultAsync(i => i.FilePath == filePath, ct);
    }
}
