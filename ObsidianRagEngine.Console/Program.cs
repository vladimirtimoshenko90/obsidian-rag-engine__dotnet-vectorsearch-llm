using Microsoft.Extensions.DependencyInjection;
using ObsidianRagEngine.Console;
using ObsidianRagEngine.Console.Composition.Llm;
using ObsidianRagEngine.Console.Composition.Ocr;
using ObsidianRagEngine.Console.Data;
using ObsidianRagEngine.Console.Domain;
using ObsidianRagEngine.Console.Domain.ObsidianNoteIngestion;
using ObsidianRagEngine.Console.Domain.ObsidianVault;

// --- Dependency injection ---
var configuration = ApplicationConfiguration.Build();

await using var serviceProvider = new ServiceCollection()
    .AddLlm(configuration)
    .AddOcr(configuration)
    .AddDataLayer(configuration)
    .AddDomainLayer(configuration)
    .AddObsidianNoteIngestion(configuration)
    .BuildServiceProvider();

// --- App ---
await serviceProvider.InitializeStorages(CancellationToken.None);

using var scope = serviceProvider.CreateScope();
var services = scope.ServiceProvider;

var vaultReader = services.GetRequiredService<IObsidianVaultReader>();
var ingestion = services.GetRequiredService<IObsidianNoteIngestionService>();

foreach (var noteInfo in vaultReader.IdentifyAllNotes())
{
    var noteFile = await vaultReader.ReadNote(noteInfo.FilePath);
    await ingestion.IngestNote(noteFile, CancellationToken.None);
}
