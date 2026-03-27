using Microsoft.EntityFrameworkCore;
using ObsidianRagEngine.Console.Data;


var connectionString = Environment.GetEnvironmentVariable("DB_CONNECTION_STRING");

var options = new DbContextOptionsBuilder<ObsidianNotesDbContext>()
    .UseNpgsql(connectionString)
    .Options;

await using var db = new ObsidianNotesDbContext(options);
await db.Database.EnsureCreatedAsync();

Console.WriteLine("Database connection established and schema ensured.");
