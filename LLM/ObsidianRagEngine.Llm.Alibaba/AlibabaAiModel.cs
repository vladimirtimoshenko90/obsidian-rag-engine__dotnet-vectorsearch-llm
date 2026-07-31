using System.ComponentModel.DataAnnotations;
using System.Reflection;
using ObsidianRagEngine.Llm.Pricing;

namespace ObsidianRagEngine.Llm.Alibaba;

// [LlmTariff] rates: per 1M tokens (input / output / cached input).
// International list ≤256K: https://www.alibabacloud.com/help/en/model-studio/model-pricing
// Cached input ≈ 10% of input (Model Studio context cache).
public enum AlibabaAiModel
{
    /// <summary>Best average; default chat/JSON/Generate; vision OCR default.</summary>
    [Display(Name = "qwen3.7-plus-2026-05-26")]
    [LlmTariff(0.4, 1.6, cachedInputPer1M: 0.04)]
    Qwen37Plus,

    /// <summary>Smartest / flagship text.</summary>
    [Display(Name = "qwen3.7-max-2026-06-08")]
    [LlmTariff(2.5, 7.5, cachedInputPer1M: 0.25)]
    Qwen37Max,
}

internal static class AlibabaAiModelExtensions
{
    public static string ToApiModelId(this AlibabaAiModel model)
    {
        var member = typeof(AlibabaAiModel).GetField(model.ToString())
            ?? throw new ArgumentOutOfRangeException(nameof(model), model, "Unknown Alibaba AI model.");

        var display = member.GetCustomAttribute<DisplayAttribute>()
            ?? throw new InvalidOperationException($"AlibabaAiModel.{model} is missing [Display(Name = \"...\")].");

        return display.Name
            ?? throw new InvalidOperationException($"AlibabaAiModel.{model} has empty Display.Name.");
    }
}
