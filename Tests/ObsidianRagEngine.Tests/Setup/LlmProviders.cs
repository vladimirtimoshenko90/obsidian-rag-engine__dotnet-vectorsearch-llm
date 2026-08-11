using ObsidianRagEngine.Llm.Alibaba;
using ObsidianRagEngine.Llm.DeepSeek;
using ObsidianRagEngine.Llm.Kimi;
using ObsidianRagEngine.Llm.OpenRouter;

namespace ObsidianRagEngine.Tests.Setup;

public enum LlmVendor
{
    DeepSeek,
    Kimi,
    Alibaba,
    OpenRouter,
}

/// <summary>Identifies one cloud LLM engine + model (no live clients).</summary>
public sealed record LlmProviderSpec(LlmVendor Vendor, string Model)
{
    public override string ToString() => $"{Vendor}:{Model}";
}

/// <summary>
/// Catalog of cloud LLM engine/model combinations used by OCR tests
/// (not DeepSeekOllama). Does not construct clients or services.
/// </summary>
public static class LlmProviders
{
    public static IReadOnlyList<LlmProviderSpec> All { get; } =
    [
        ..Enum.GetValues<DeepSeekAiModel>().Select(m => new LlmProviderSpec(LlmVendor.DeepSeek, m.ToString())),
        ..Enum.GetValues<KimiAiModel>().Select(m => new LlmProviderSpec(LlmVendor.Kimi, m.ToString())),
        ..Enum.GetValues<AlibabaAiModel>().Select(m => new LlmProviderSpec(LlmVendor.Alibaba, m.ToString())),
        ..Enum.GetValues<OpenRouterAiModel>().Select(m => new LlmProviderSpec(LlmVendor.OpenRouter, m.ToString()))
    ];

    /// <summary>Vendors that implement vision <c>IOcrProvider</c> (Kimi, Alibaba).</summary>
    public static IReadOnlyList<LlmProviderSpec> OcrCapable { get; } =
    [
        ..Enum.GetValues<KimiAiModel>().Select(m => new LlmProviderSpec(LlmVendor.Kimi, m.ToString())),
        ..Enum.GetValues<AlibabaAiModel>().Select(m => new LlmProviderSpec(LlmVendor.Alibaba, m.ToString())),
    ];
}
