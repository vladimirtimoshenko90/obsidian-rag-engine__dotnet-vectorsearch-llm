using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ObsidianRagEngine.Console.Composition.Llm;
using ObsidianRagEngine.Console.Composition.Ocr;
using ObsidianRagEngine.Console.Data;
using ObsidianRagEngine.Console.Domain.ObsidianNoteIngestion;
using ObsidianRagEngine.Console.Domain.ObsidianVault;

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
services.AddLlm(configuration);
services.AddObsidianNoteIngestion(configuration);
await using var serviceProvider = services.BuildServiceProvider();
using var scope = serviceProvider.CreateScope();
var sp = scope.ServiceProvider;

await sp.InitializeStorages(CancellationToken.None);

// --- App ---
var vaultPath = configuration["ObsidianVault:Path"]!;
var attachmentsFolder = configuration["ObsidianVault:AttachmentsFolder"]!;
var vaultReader = new ObsidianVaultReader(vaultPath, attachmentsFolder);
var processingService = sp.GetRequiredService<IObsidianNoteIngestionService>();

var noteInfos = vaultReader.IdentifyAllNotes();
foreach (var noteInfo in noteInfos)
{
    var noteFile = await vaultReader.ReadNote(noteInfo.FilePath);
    await processingService.IngestNote(noteFile, CancellationToken.None);
}
