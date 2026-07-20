using System.Net.Http.Json;
using System.Text.Json;

namespace ObsidianRagEngine.Console.Domain.Ocr;

public interface IImageOcrService
{
    string ModelName { get; }
    Task<string> ExtractText(byte[] imageBytes, IReadOnlyList<string> languages);
}

/// <summary>
/// Client for the docker-hosted Tesseract HTTP OCR server:
/// https://github.com/hertzg/tesseract-server/
/// </summary>
public class TesseractOllamaService(HttpClient httpClient) : IImageOcrService
{
    public string ModelName => "tesseract";

    public async Task<string> ExtractText(byte[] imageBytes, IReadOnlyList<string> languages)
    {
        var optionsJson = JsonSerializer.Serialize(new
        {
            languages,
            configParams = new { }
        });

        var content = new MultipartFormDataContent
        {
            { new ByteArrayContent(imageBytes), "file", "image.png" },
            { new StringContent(optionsJson), "options" }
        };

        var response = await httpClient.PostAsync("/tesseract", content);
        response.EnsureSuccessStatusCode();

        var wrapper = await response.Content.ReadFromJsonAsync<TesseractWrapper>();
        var data = wrapper?.Data
            ?? throw new TesseractException("Tesseract response was empty or malformed.");

        if (data.Exit.Code != 0 || data.Exit.Signal is not null)
        {
            throw new TesseractException(
                $"Tesseract process failed (exit code: {data.Exit.Code}, signal: {data.Exit.Signal ?? "none"}).",
                data.Exit.Code,
                data.Exit.Signal,
                data.Stderr);
        }

        return data.Stdout?.Trim() ?? string.Empty;
    }

    private sealed record TesseractWrapper(TesseractData Data);
    private sealed record TesseractData(ExitInfo Exit, string? Stdout, string? Stderr);
    private sealed record ExitInfo(int? Code, string? Signal);
}
