using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace ObsidianRagEngine.Console.Domain;

public interface ILlmService
{
    Task<string> GenerateResponse(string prompt, CancellationToken ct = default);
}

public class OllamaLlmService(HttpClient httpClient, string modelName) : ILlmService
{
    public async Task<string> GenerateResponse(string prompt, CancellationToken ct = default)
    {
        var response = await httpClient.PostAsJsonAsync(
            "/api/generate",
            new OllamaGenerateRequest(modelName, prompt, Stream: false),
            ct);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<OllamaGenerateResponse>(ct);
        return result!.Response ?? string.Empty;
    }

    private sealed record OllamaGenerateRequest(
        [property: JsonPropertyName("model")] string Model,
        [property: JsonPropertyName("prompt")] string Prompt,
        [property: JsonPropertyName("stream")] bool Stream);

    private sealed record OllamaGenerateResponse(
        [property: JsonPropertyName("response")] string? Response);
}
