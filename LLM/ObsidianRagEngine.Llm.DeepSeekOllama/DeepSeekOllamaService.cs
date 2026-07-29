using System.Net.Http.Json;
using System.Text.Json.Serialization;
using ObsidianRagEngine.Contracts;

namespace ObsidianRagEngine.Llm.DeepSeekOllama;

public class DeepSeekOllamaService(HttpClient httpClient, string modelName) 
    : ILlmProvider
{
    public string ModelName => modelName;

    public async Task<string> Complete(string prompt, CancellationToken ct)
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
