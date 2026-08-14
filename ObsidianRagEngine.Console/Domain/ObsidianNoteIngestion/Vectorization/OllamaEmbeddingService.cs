using System.Net.Http.Json;
using Microsoft.Extensions.Options;

namespace ObsidianRagEngine.Console.Domain.ObsidianNoteIngestion.Vectorization;

public interface IEmbeddingService
{
    string ModelName { get; }
    Task<float[]> Embed(string text, CancellationToken ct = default);
}

public class OllamaEmbeddingService(HttpClient httpClient, IOptions<OllamaEmbeddingSettings> settings)
    : IEmbeddingService
{
    public string ModelName => settings.Value.EmbeddingModel;

    public async Task<float[]> Embed(string text, CancellationToken ct = default)
    {
        var response = await httpClient.PostAsJsonAsync(
            "/api/embed",
            new OllamaEmbeddingRequest(ModelName, text),
            ct);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<OllamaEmbeddingResponse>(ct);
        return result!.Embeddings.First();
    }

    private sealed record OllamaEmbeddingRequest(string Model, string Input);
    private sealed record OllamaEmbeddingResponse(float[][] Embeddings);
}

public sealed class OllamaEmbeddingSettings
{
    public string Url { get; set; } = string.Empty;
    public string EmbeddingModel { get; set; } = string.Empty;
}
