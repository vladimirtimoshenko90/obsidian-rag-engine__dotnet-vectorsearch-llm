using Microsoft.Extensions.DependencyInjection;
using ObsidianRagEngine.Console.Data.ObsidianNoteChunks.Repositories;
using ObsidianRagEngine.Console.Data.ObsidianNotes;
using Qdrant.Client;
using Qdrant.Client.Grpc;

namespace ObsidianRagEngine.Console.Data;

public static class ApplicationStartup
{
    private const uint EmbeddingDimension = 768;

    public static async Task InitializeStorages(this IServiceProvider services, CancellationToken ct)
    {
        var db = services.GetRequiredService<ObsidianNotesDbContext>();
        await db.Database.EnsureCreatedAsync(ct);
        System.Console.WriteLine("PostgreSQL: connection established and schema ensured.");

        var qdrantClient = services.GetRequiredService<QdrantClient>();
        var collectionExists = await qdrantClient.CollectionExistsAsync(
            ObsidianNoteChunkRepository.CollectionName,
            cancellationToken: ct);

        if (!collectionExists)
        {
            await qdrantClient.CreateCollectionAsync(
                ObsidianNoteChunkRepository.CollectionName,
                new VectorParams { Size = EmbeddingDimension, Distance = Distance.Cosine },
                cancellationToken: ct);
        }

        System.Console.WriteLine($"Qdrant: collection '{ObsidianNoteChunkRepository.CollectionName}' ensured.");
    }
}
