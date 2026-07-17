using System.Net.Http.Json;
using System.Text.Json;

namespace ObsidianRagEngine.Console.Domain;

public interface IImageOcrService
{
    string ModelName { get; }
    Task<string> ExtractText(byte[] imageBytes, IReadOnlyList<string> languages);
}

// Client for the docker-hosted Tesseract HTTP OCR server:
// https://github.com/hertzg/tesseract-server/
public class TesseractOcrService(HttpClient httpClient) : IImageOcrService
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
        return wrapper?.Data?.Stdout?.Trim() ?? string.Empty;
    }

    private sealed record TesseractWrapper(TesseractData Data);
    private sealed record TesseractData(ExitInfo Exit, string Stdout, string Stderr);
    private sealed record ExitInfo(int Code, object? Signal);
}

// Alpine tesseract-ocr-data packages (available language pack IDs):
// https://pkgs.alpinelinux.org/packages?name=tesseract-ocr-data-*&branch=edge
public static class TesseractLanguages
{
    public const string English = "eng";
    public const string German = "deu";
    public const string French = "fra";
    public const string Georgian = "kat";
    public const string Polish = "pol";
    public const string Russian = "rus";
    public const string Spanish = "spa";
}
