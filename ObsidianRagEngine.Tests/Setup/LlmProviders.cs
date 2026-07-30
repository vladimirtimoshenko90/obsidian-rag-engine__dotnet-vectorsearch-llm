using ObsidianRagEngine.Contracts;
using ObsidianRagEngine.Llm.Alibaba;
using ObsidianRagEngine.Llm.DeepSeek;
using ObsidianRagEngine.Llm.Kimi;
using OpenAI;
using System.ClientModel;

namespace ObsidianRagEngine.Tests.Setup;

/// <summary>
/// Every cloud <see cref="ILlmProvider"/> used by Messenger OCR tests:
/// one instance per DeepSeek / Kimi / Alibaba model enum value (not DeepSeekOllama).
/// </summary>
public static class LlmProviders
{
    public static IReadOnlyList<ILlmProvider> All => AllLazy.Value;

    private static readonly Lazy<IReadOnlyList<ILlmProvider>> AllLazy = new(() =>
    {
        var providers = new List<ILlmProvider>();

        var deepSeekOpenAiClient = CreateClient(TestEnvironmentSettings.DeepSeek);
        foreach (var model in Enum.GetValues<DeepSeekAiModel>())
            providers.Add(new DeepSeekService(deepSeekOpenAiClient, model));

        var kimiOpenAiClient = CreateClient(TestEnvironmentSettings.Kimi);
        foreach (var model in Enum.GetValues<KimiAiModel>())
            providers.Add(new KimiService(kimiOpenAiClient, model));

        var alibabaOpenAiClient = CreateClient(TestEnvironmentSettings.Alibaba);
        foreach (var model in Enum.GetValues<AlibabaAiModel>())
            providers.Add(new AlibabaService(alibabaOpenAiClient, model));

        return providers;
    });

    private static OpenAIClient CreateClient(OpenAiCompatibleSettings settings) =>
        new(
            new ApiKeyCredential(settings.ApiKey),
            new OpenAIClientOptions
            {
                Endpoint = settings.Endpoint,
                NetworkTimeout = TimeSpan.FromMinutes(10),
            });
}
