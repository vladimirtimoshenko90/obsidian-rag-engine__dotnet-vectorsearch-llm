using Microsoft.EntityFrameworkCore;
using ObsidianRagEngine.Console.Data.Entities;

namespace ObsidianRagEngine.Console.Data;

public class ObsidianNotesDbContext(DbContextOptions<ObsidianNotesDbContext> options)
    : DbContext(options)
{
    public DbSet<ObsidianNote> ObsidianNotes => Set<ObsidianNote>();
}
