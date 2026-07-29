using ObsidianRagEngine.Llm.DeepSeekOllama;
using ObsidianRagEngine.Ocr.Messaging;
using ObsidianRagEngine.Ocr.Tesseract;

namespace ObsidianRagEngine.Tests.Setup;

/// <summary>
/// xUnit class fixture: shared SetUp for OCR test classes, Dispose is TearDown.
/// </summary>
public sealed class OcrFixture : IDisposable
{
    public TestSettingsFixture Settings { get; } = new();

    public TesseractOcrService Tesseract { get; }

    public MessengerScreenshotOcrService MessengerScreenshot { get; }

    private readonly HttpClient _tesseractHttpClient;
    private readonly HttpClient _ollamaHttpClient;

    public OcrFixture()
    {
        _tesseractHttpClient = new HttpClient
        {
            BaseAddress = new Uri(Settings.TesseractUrl),
            Timeout = TimeSpan.FromMinutes(2)
        };

        _ollamaHttpClient = new HttpClient
        {
            BaseAddress = new Uri(Settings.OllamaUrl),
            Timeout = TimeSpan.FromMinutes(5)
        };

        var llm = new DeepSeekOllamaService(_ollamaHttpClient, Settings.OllamaLlmModel);

        Tesseract = new TesseractOcrService(_tesseractHttpClient);

        MessengerScreenshot = new MessengerScreenshotOcrService(
            Tesseract,
            llm,
            new MessengerPanelSplitter(),
            new MessengerPanelNormalizer());
    }

    public void Dispose()
    {
        TestSettingsFixture.ConsolidateOcrResults();
        _tesseractHttpClient.Dispose();
        _ollamaHttpClient.Dispose();
    }
}
