using ObsidianRagEngine.Contracts;
using ObsidianRagEngine.Llm.Alibaba;
using ObsidianRagEngine.Llm.DeepSeek;
using ObsidianRagEngine.Llm.Kimi;
using ObsidianRagEngine.Llm.OpenRouter;
using ObsidianRagEngine.Ocr.Instruments.Tesseract;
using OpenAI;
using System.ClientModel;

namespace ObsidianRagEngine.Tests.Setup;

public sealed class OcrFixture : IDisposable
{
    private readonly HttpClient _tesseractHttpClient;
    public TesseractOcrService Tesseract { get; }

    private readonly Dictionary<LlmVendor, OpenAIClient> _openAiClients = new();
    private readonly Dictionary<LlmProviderSpec, ILlmProvider> _llmProviders = new();

    public OcrFixture()
    {
        _tesseractHttpClient = new HttpClient
        {
            BaseAddress = new Uri(TestEnvironmentSettings.TesseractUrl),
            Timeout = TimeSpan.FromMinutes(2)
        };
        Tesseract = new TesseractOcrService(_tesseractHttpClient);
    }

    public ILlmProvider GetLlmProvider(LlmProviderSpec spec)
    {
        if (_llmProviders.TryGetValue(spec, out var existingProvider))
            return existingProvider;

        if (!_openAiClients.TryGetValue(spec.Vendor, out var openAiClient))
        {
            var openAiSettings = spec.Vendor switch
            {
                LlmVendor.DeepSeek => TestEnvironmentSettings.DeepSeek,
                LlmVendor.Kimi => TestEnvironmentSettings.Kimi,
                LlmVendor.Alibaba => TestEnvironmentSettings.Alibaba,
                LlmVendor.OpenRouter => TestEnvironmentSettings.OpenRouter,
                _ => throw new ArgumentOutOfRangeException(nameof(spec), spec.Vendor, "Unknown LLM vendor."),
            };
            _openAiClients[spec.Vendor] = openAiClient = new OpenAIClient(
                new ApiKeyCredential(openAiSettings.ApiKey),
                new OpenAIClientOptions
                {
                    Endpoint = openAiSettings.Endpoint,
                    NetworkTimeout = TimeSpan.FromMinutes(10),
                });
        }

        ILlmProvider provider = spec.Vendor switch
        {
            LlmVendor.DeepSeek => new DeepSeekService(openAiClient, Enum.Parse<DeepSeekAiModel>(spec.Model)),
            LlmVendor.Kimi => new KimiService(openAiClient, Enum.Parse<KimiAiModel>(spec.Model)),
            LlmVendor.Alibaba => new AlibabaService(openAiClient, Enum.Parse<AlibabaAiModel>(spec.Model)),
            LlmVendor.OpenRouter => new OpenRouterService(openAiClient, Enum.Parse<OpenRouterAiModel>(spec.Model)),
            _ => throw new ArgumentOutOfRangeException(nameof(spec), spec.Vendor, "Unknown LLM vendor."),
        };

        _llmProviders[spec] = provider;
        return provider;
    }

    public IOcrProvider? GetOcrProvider(LlmProviderSpec llmSpec)
        => GetLlmProvider(llmSpec) as IOcrProvider;

    public void Dispose()
    {
        _tesseractHttpClient.Dispose();
        OcrTestStore.ConsolidateResults();
    }
}
