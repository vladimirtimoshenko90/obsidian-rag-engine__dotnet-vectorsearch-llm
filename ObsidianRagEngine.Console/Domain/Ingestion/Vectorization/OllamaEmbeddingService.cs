using System.Net.Http.Json;

namespace ObsidianRagEngine.Console.Domain.Ingestion.Vectorization;

public interface IEmbeddingService
{
    string ModelName { get; }
    Task<float[]> Embed(string text, CancellationToken ct = default);
}

public class OllamaEmbeddingService(HttpClient httpClient, string modelName) : IEmbeddingService
{
    public string ModelName => modelName;

    public async Task<float[]> Embed(string text, CancellationToken ct = default)
    {
        var response = await httpClient.PostAsJsonAsync(
            "/api/embed",
            new OllamaEmbeddingRequest(modelName, text),
            ct);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<OllamaEmbeddingResponse>(ct);
        return result!.Embeddings.First();
    }

    private sealed record OllamaEmbeddingRequest(string Model, string Input);
    private sealed record OllamaEmbeddingResponse(float[][] Embeddings);
}
