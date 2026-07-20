using ObsidianRagEngine.Console.Domain.Ocr;

namespace ObsidianRagEngine.Tests.Setup;

/// <summary>
/// xUnit class fixture: shared SetUp for the test class, Dispose is TearDown.
/// </summary>
public sealed class TesseractOllamaFixture : IDisposable
{
    public TestSettingsFixture Settings { get; } = new();

    public TesseractOllamaService Sut { get; }

    private readonly HttpClient _httpClient;

    public TesseractOllamaFixture()
    {
        _httpClient = new HttpClient
        {
            BaseAddress = new Uri(Settings.TesseractUrl),
            Timeout = TimeSpan.FromMinutes(2)
        };
        Sut = new TesseractOllamaService(_httpClient);
    }

    public void Dispose() => _httpClient.Dispose();
}
