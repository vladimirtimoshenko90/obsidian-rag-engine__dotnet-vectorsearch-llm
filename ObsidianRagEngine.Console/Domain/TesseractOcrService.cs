using System.Net.Http.Json;

namespace ObsidianRagEngine.Console.Domain;

public interface IImageOcrService
{
    string ModelName { get; }
    Task<string> ExtractText(byte[] imageBytes, string language = "rus");
}

public class TesseractOcrService(HttpClient httpClient) : IImageOcrService
{
    public string ModelName => "tesseract";
    public async Task<string> ExtractText(byte[] imageBytes, string language = "rus")
    {
        var optionsJson = $"{{\"languages\": [\"{language}\"]}}";

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