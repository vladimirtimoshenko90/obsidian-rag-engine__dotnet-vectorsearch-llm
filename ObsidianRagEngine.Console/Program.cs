using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ObsidianRagEngine.Console.Composition.Ocr;
using ObsidianRagEngine.Console.Data;
using ObsidianRagEngine.Console.Data.ObsidianNoteChunks.Repositories;
using ObsidianRagEngine.Console.Data.ObsidianNotes.Repositories;
using ObsidianRagEngine.Console.Domain.ObsidianNoteIngestion;
using ObsidianRagEngine.Console.Domain.ObsidianNoteIngestion.Sanitization;
using ObsidianRagEngine.Console.Domain.ObsidianNoteIngestion.Vectorization;
using ObsidianRagEngine.Console.Domain.Reading;
using ObsidianRagEngine.Contracts;
using ObsidianRagEngine.Llm.DeepSeekOllama;

Console.OutputEncoding = System.Text.Encoding.UTF8;

var environment = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT")
    ?? Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")
    ?? "Production";

var configuration = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
    .AddJsonFile($"appsettings.{environment}.json", optional: true, reloadOnChange: false)
    .AddEnvironmentVariables()
    .Build();

var services = new ServiceCollection();
services.AddDataLayer(configuration);
services.AddOcr(configuration);
await using var serviceProvider = services.BuildServiceProvider();
using var scope = serviceProvider.CreateScope();
var sp = scope.ServiceProvider;

await sp.InitializeStorages(CancellationToken.None);

// --- App ---
var obsidianRepositoryPath = configuration["ObsidianRepository:Path"]!;
var attachmentsFolder = configuration["ObsidianRepository:AttachmentsFolder"]!;
var obsidianRepo = new ObsidianRepositoryReader(obsidianRepositoryPath, attachmentsFolder);

var noteRepo = sp.GetRequiredService<IObsidianNoteRepository>();
var imageRepo = sp.GetRequiredService<IObsidianImageRepository>();
var chunkRepo = sp.GetRequiredService<IObsidianNoteChunkRepository>();

var ocrService = sp.GetRequiredService<IOcrProvider>();

var ollamaUrl = configuration["Ollama:Url"]!;
var ollamaEmbeddingModel = configuration["Ollama:EmbeddingModel"]!;
var embeddingService = new OllamaEmbeddingService(new HttpClient { BaseAddress = new Uri(ollamaUrl) }, ollamaEmbeddingModel);

var ollamaLlmModel = configuration["Ollama:LlmModel"]!;
var llmService = new DeepSeekOllamaService(new HttpClient { BaseAddress = new Uri(ollamaUrl) }, ollamaLlmModel);

var chunkingService = new TextChunkingService();
var vectorizationService = new ObsidianNoteVectorizationService(chunkRepo, chunkingService, embeddingService);

var noteSanitization = new ObsidianNoteSanitizationService(imageRepo, ocrService);
var processingService = new ObsidianNoteIngestionService(noteRepo, chunkRepo, noteSanitization, vectorizationService);

var noteInfos = obsidianRepo.IdentifyAllNotes();
foreach (var noteInfo in noteInfos)
{
    var noteFile = await obsidianRepo.ReadNote(noteInfo.FilePath);
    await processingService.IngestNote(noteFile, CancellationToken.None);
}
