using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using ObsidianRagEngine.Console.Data;

var configuration = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
    .Build();

var connectionString = configuration.GetConnectionString("ObsidianNotes");

var options = new DbContextOptionsBuilder<ObsidianNotesDbContext>()
    .UseNpgsql(connectionString)
    .Options;

await using var db = new ObsidianNotesDbContext(options);
await db.Database.EnsureCreatedAsync();

Console.WriteLine("Database connection established and schema ensured.");
