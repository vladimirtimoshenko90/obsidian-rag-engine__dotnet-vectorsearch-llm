using ObsidianRagEngine.Ocr.Tesseract;

namespace ObsidianRagEngine.Tests.Setup;

public sealed class OcrFixture : IDisposable
{
    private readonly HttpClient _tesseractHttpClient;
    public TesseractOcrService Tesseract { get; }

    public OcrFixture()
    {
        _tesseractHttpClient = new HttpClient
        {
            BaseAddress = new Uri(TestEnvironmentSettings.TesseractUrl),
            Timeout = TimeSpan.FromMinutes(2)
        };
        Tesseract = new TesseractOcrService(_tesseractHttpClient);
    }

    public void Dispose()
    {
        _tesseractHttpClient.Dispose();
        OcrTestStore.ConsolidateResults();
    }
}
