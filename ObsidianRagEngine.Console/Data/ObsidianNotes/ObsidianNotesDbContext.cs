using Microsoft.EntityFrameworkCore;
using ObsidianRagEngine.Console.Data.ObsidianNotes.Entities;

namespace ObsidianRagEngine.Console.Data.ObsidianNotes;

public class ObsidianNotesDbContext(DbContextOptions<ObsidianNotesDbContext> options)
    : DbContext(options)
{
    public DbSet<ObsidianNote> ObsidianNotes => Set<ObsidianNote>();
}
