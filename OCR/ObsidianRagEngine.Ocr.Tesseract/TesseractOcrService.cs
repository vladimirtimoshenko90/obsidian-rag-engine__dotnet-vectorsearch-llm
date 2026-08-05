using ObsidianRagEngine.Contracts;
using ObsidianRagEngine.Ocr.Tesseract.Exceptions;
using ObsidianRagEngine.Ocr.Tesseract.Languages;
using System.Net.Http.Json;
using System.Text.Json;

namespace ObsidianRagEngine.Ocr.Tesseract;

/// <summary>
/// Client for the docker-hosted Tesseract HTTP OCR server:
/// https://github.com/hertzg/tesseract-server/
/// </summary>
public class TesseractOcrService(HttpClient httpClient) : IOcrProvider
{
    public string ModelName => "tesseract";

    public async Task<LlmCallResult> ExtractText(
        byte[] imageBytes,
        IReadOnlyList<OcrLanguage> languages,
        CancellationToken ct,
        string? clarificationPrompt = null)
    {
        var optionsJson = JsonSerializer.Serialize(new
        {
            languages = TesseractLanguageMapper.ToTesseractCodes(languages),
            configParams = new { }
        });

        var content = new MultipartFormDataContent
        {
            { new ByteArrayContent(imageBytes), "file", "image.png" },
            { new StringContent(optionsJson), "options" }
        };

        var response = await httpClient.PostAsync("/tesseract", content, ct);
        response.EnsureSuccessStatusCode();

        var wrapper = await response.Content.ReadFromJsonAsync<TesseractWrapper>(ct);
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

        return new LlmCallResult(data.Stdout?.Trim() ?? string.Empty, Cost: 0m, LlmTokenUsage.Zero);
    }

    private sealed record TesseractWrapper(TesseractData Data);
    private sealed record TesseractData(ExitInfo Exit, string? Stdout, string? Stderr);
    private sealed record ExitInfo(int? Code, string? Signal);
}
