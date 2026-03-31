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
const uint EmbeddingDimension = 768;

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
var obsidianRepo = new ObsidianRepositoryReader(obsidianRepositoryPath, attachmentsFolder);

var noteRepo = new ObsidianNoteRepository(db);
var imageRepo = new ObsidianImageRepository(db);

var tesseractUrl = configuration["Tesseract:Url"]!;
var ocrService = new TesseractOcrService(new HttpClient { BaseAddress = new Uri(tesseractUrl) });

var processingService = new ObsidianNoteIndexingService(noteRepo, imageRepo, ocrService);

var ollamaUrl = configuration["Ollama:Url"]!;
var ollamaEmbeddingModel = configuration["Ollama:EmbeddingModel"]!;
var embeddingService = new OllamaEmbeddingService(new HttpClient { BaseAddress = new Uri(ollamaUrl) }, ollamaEmbeddingModel);

var ollamaLlmModel = configuration["Ollama:LlmModel"]!;
var llmService = new OllamaLlmService(new HttpClient { BaseAddress = new Uri(ollamaUrl) }, ollamaLlmModel);

var chunkRepo = new ObsidianNoteChunkRepository(qdrantClient);
var chunkingService = new TextChunkingService();
var vectorizationService = new ObsidianNoteVectorizationService(chunkRepo, chunkingService, embeddingService);

var noteInfos = obsidianRepo.IdentifyAllNotes();
foreach (var noteInfo in noteInfos)
{
    var noteFile = await obsidianRepo.ReadNote(noteInfo.FilePath);
    var note = await processingService.ProcessNote(noteFile);
    await vectorizationService.VectorizeNote(note);
}