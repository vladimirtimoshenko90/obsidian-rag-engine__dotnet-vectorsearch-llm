using ObsidianRagEngine.Llm.DeepSeekOllama;
using ObsidianRagEngine.Ocr.Pipelines.Messenger;
using ObsidianRagEngine.Ocr.Tesseract;

namespace ObsidianRagEngine.Tests.Setup;

public sealed class OcrFixture : IDisposable
{
    private readonly HttpClient _tesseractHttpClient;
    public TesseractOcrService Tesseract { get; }

    private readonly HttpClient _ollamaHttpClient;
    private readonly DeepSeekOllamaService _llm;
    public MessengerOcrService MessengerScreenshot { get; }

    public OcrFixture()
    {
        _tesseractHttpClient = new HttpClient
        {
            BaseAddress = new Uri(TestEnvironmentSettings.TesseractUrl),
            Timeout = TimeSpan.FromMinutes(2)
        };
        Tesseract = new TesseractOcrService(_tesseractHttpClient);

        _ollamaHttpClient = new HttpClient
        {
            BaseAddress = new Uri(TestEnvironmentSettings.OllamaUrl),
            Timeout = TimeSpan.FromMinutes(5)
        };
        _llm = new DeepSeekOllamaService(_ollamaHttpClient, TestEnvironmentSettings.OllamaLlmModel);
        MessengerScreenshot = new MessengerOcrService(Tesseract, _llm);
    }

    public void Dispose()
    {
        _tesseractHttpClient.Dispose();
        _ollamaHttpClient.Dispose();

        OcrTestStore.ConsolidateResults();
    }
}
