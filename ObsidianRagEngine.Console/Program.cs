using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using ObsidianRagEngine.Console.Data.ObsidianNoteChunks.Repositories;
using ObsidianRagEngine.Console.Data.ObsidianNotes;
using ObsidianRagEngine.Console.Domain;
using Qdrant.Client;
using Qdrant.Client.Grpc;

Console.OutputEncoding = System.Text.Encoding.UTF8;

var configuration = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
    .AddEnvironmentVariables()
.Build();

// --- PostgreSQL setup ---
var connectionString = configuration.GetConnectionString("ObsidianNotes");

var dbOptions = new DbContextOptionsBuilder<ObsidianNotesDbContext>()
    .UseNpgsql(connectionString)
    .Options;

await using var db = new ObsidianNotesDbContext(dbOptions);
await db.Database.EnsureCreatedAsync();

Console.WriteLine("PostgreSQL: connection established and schema ensured.");

// --- Qdrant setup ---
const uint EmbeddingDimension = 4;

var qdrantUri = new Uri(configuration.GetConnectionString("ObsidianNoteChunks")!);

var qdrantClient = new QdrantClient(qdrantUri);

var collectionExists = await qdrantClient.CollectionExistsAsync(ObsidianNoteChunkRepository.CollectionName);
if (!collectionExists)
{
    await qdrantClient.CreateCollectionAsync(ObsidianNoteChunkRepository.CollectionName,
        new VectorParams { Size = EmbeddingDimension, Distance = Distance.Cosine });
}

Console.WriteLine($"Qdrant: collection '{ObsidianNoteChunkRepository.CollectionName}' ensured.");

// --- App ---
var obsidianRepositoryPath = configuration["ObsidianRepository:Path"]!;
var repositoryReader = new ObsidianRepositoryReader(obsidianRepositoryPath);

var notes = repositoryReader.IdentifyAllNotes();
Console.WriteLine("Obsidian notes:");
foreach (var note in notes)
{
    Console.WriteLine($"  - {note.Name} / {note.FilePath}");
}
