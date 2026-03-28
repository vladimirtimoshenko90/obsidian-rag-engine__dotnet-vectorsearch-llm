using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using ObsidianRagEngine.Console.Data.ObsidianNoteChunks.Repositories;
using ObsidianRagEngine.Console.Data.ObsidianNotes;
using ObsidianRagEngine.Console.Data.ObsidianNotes.Repositories;
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
var attachmentsFolder = configuration["ObsidianRepository:AttachmentsFolder"]!;
var repositoryReader = new ObsidianRepositoryReader(obsidianRepositoryPath, attachmentsFolder);

var noteRepo = new ObsidianNoteRepository(db);
var imageRepo = new ObsidianImageRepository(db);
var processingService = new ObsidianNoteProcessingService(repositoryReader, noteRepo, imageRepo);

var notes = repositoryReader.IdentifyAllNotes();
foreach (var note in notes)
{
    await processingService.ProcessNote(note);
    Console.WriteLine($"Processed: {note.Name}");
}

var tesseractUrl = configuration["Tesseract:Url"]!;
var ocrService = new TesseractOcrService(new HttpClient { BaseAddress = new Uri(tesseractUrl) });

var allImages = repositoryReader.IdentifyAllImages();
var first10 = allImages.Take(10).ToList();

foreach (var imageFileName in first10)
{
    var imageFilePath = Path.Combine(obsidianRepositoryPath, attachmentsFolder, imageFileName);
    var imageBytes = await File.ReadAllBytesAsync(imageFilePath);
    var extractedText = await ocrService.ExtractText(imageBytes);
    Console.WriteLine($"[{imageFilePath}]{Environment.NewLine}{extractedText}{Environment.NewLine}");
    Console.WriteLine("---------------\n");
}