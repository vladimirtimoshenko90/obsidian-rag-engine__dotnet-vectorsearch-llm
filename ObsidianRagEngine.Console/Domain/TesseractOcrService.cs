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
        var data = wrapper?.Data
            ?? throw new TesseractOcrException("Tesseract response was empty or malformed.");

        if (data.Exit.Code != 0 || data.Exit.Signal is not null)
        {
            throw new TesseractOcrException(
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

public class TesseractOcrException : Exception
{
    public int? ExitCode { get; }
    public string? Signal { get; }
    public string? Stderr { get; }

    public TesseractOcrException(string message, int? exitCode = null, string? signal = null, string? stderr = null)
        : base(BuildMessage(message, stderr))
    {
        ExitCode = exitCode;
        Signal = signal;
        Stderr = stderr;
    }

    private static string BuildMessage(string message, string? stderr) =>
        string.IsNullOrWhiteSpace(stderr) ? message : $"{message} Stderr: {stderr.Trim()}";
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
