using Qdrant.Client;
using Qdrant.Client.Grpc;
using ObsidianRagEngine.Console.Data.ObsidianNoteChunks.Entities;

namespace ObsidianRagEngine.Console.Data.ObsidianNoteChunks.Repositories;

public interface IObsidianNoteChunkRepository
{
    Task Create(ObsidianNoteChunk chunk, CancellationToken ct = default);
    Task Delete(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<ObsidianNoteChunk>> GetByNoteId(int noteId, CancellationToken ct = default);
}

public class ObsidianNoteChunkRepository(QdrantClient qdrant, string collectionName)
    : IObsidianNoteChunkRepository
{
    public async Task Create(ObsidianNoteChunk chunk, CancellationToken ct = default)
    {
        var point = new PointStruct
        {
            Id = new PointId { Uuid = chunk.Id.ToString() },
            Vectors = new Vectors { Vector = chunk.Embedding },
            Payload =
            {
                ["note_id"] = chunk.NoteId,
                ["text"] = chunk.Text
            }
        };

        await qdrant.UpsertAsync(collectionName, [point], cancellationToken: ct);
    }

    public async Task Delete(Guid id, CancellationToken ct = default)
    {
        await qdrant.DeleteAsync(collectionName, id, cancellationToken: ct);
    }

    public async Task<IReadOnlyList<ObsidianNoteChunk>> GetByNoteId(int noteId, CancellationToken ct = default)
    {
        var filter = new Filter
        {
            Must =
            {
                new Condition
                {
                    Field = new FieldCondition
                    {
                        Key = "note_id",
                        Match = new Match { Integer = noteId }
                    }
                }
            }
        };

        var points = await qdrant.ScrollAsync(
            collectionName,
            filter,
            limit: 1000,
            vectorsSelector: new WithVectorsSelector { Enable = true },
            cancellationToken: ct);

        return points.Result.Select(ToChunk).ToList();
    }

    private static ObsidianNoteChunk ToChunk(RetrievedPoint point) => new()
    {
        Id = Guid.Parse(point.Id.Uuid),
        NoteId = (int)point.Payload["note_id"].IntegerValue,
        Text = point.Payload["text"].StringValue,
        Embedding = [.. point.Vectors.Vector.GetDenseVector()!.Data]
    };
}
