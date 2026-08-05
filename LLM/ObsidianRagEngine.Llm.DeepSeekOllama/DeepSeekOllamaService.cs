using System.Net.Http.Json;
using System.Text.Json.Serialization;
using ObsidianRagEngine.Contracts;

namespace ObsidianRagEngine.Llm.DeepSeekOllama;

public class DeepSeekOllamaService(HttpClient httpClient, string modelName) 
    : ILlmProvider
{
    public string ModelName => modelName;

    public async Task<LlmCallResult> Complete(string prompt, CancellationToken ct, bool thinkingMode = false)
    {
        // Ollama /api/generate has no thinking toggle; thinkingMode is ignored.
        var response = await httpClient.PostAsJsonAsync(
            "/api/generate",
            new OllamaGenerateRequest(modelName, prompt, Stream: false),
            ct);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<OllamaGenerateResponse>(ct);
        return new LlmCallResult(result!.Response ?? string.Empty, Cost: 0m, LlmTokenUsage.Zero);
    }

    private sealed record OllamaGenerateRequest(
        [property: JsonPropertyName("model")] string Model,
        [property: JsonPropertyName("prompt")] string Prompt,
        [property: JsonPropertyName("stream")] bool Stream);

    private sealed record OllamaGenerateResponse(
        [property: JsonPropertyName("response")] string? Response);
}
