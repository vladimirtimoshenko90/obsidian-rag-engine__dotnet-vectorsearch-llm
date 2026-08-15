using Microsoft.EntityFrameworkCore;
using ObsidianRagEngine.Console.Data.ObsidianNotes.Entities;

namespace ObsidianRagEngine.Console.Data.ObsidianNotes;

public class ObsidianNotesDbContext(DbContextOptions<ObsidianNotesDbContext> options)
    : DbContext(options)
{
    public DbSet<ObsidianNote> ObsidianNotes => Set<ObsidianNote>();
    public DbSet<ObsidianImage> ObsidianImages => Set<ObsidianImage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ObsidianNote>().Property(n => n.Cost).HasPrecision(18, 6);
        modelBuilder.Entity<ObsidianImage>().Property(i => i.Cost).HasPrecision(18, 6);
    }
}
