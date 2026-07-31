using System.ComponentModel.DataAnnotations;
using System.Reflection;
using ObsidianRagEngine.Llm.Pricing;

namespace ObsidianRagEngine.Llm.Kimi;

// [LlmTariff] rates: per 1M tokens (input / output / cached input).
// Pricing: https://platform.kimi.ai/docs/pricing
public enum KimiAiModel
{
    /// <summary>Cheapest among current multimodal; best general / average use. Hybrid thinking (off by default).</summary>
    [Display(Name = "kimi-k2.6")]
    [LlmTariff(0.95, 4.0, cachedInputPer1M: 0.16)]
    K2_6,

    /// <summary>Smartest / flagship. Thinking always on; effort low (default) or max.</summary>
    [Display(Name = "kimi-k3")]
    [LlmTariff(3.0, 15.0, cachedInputPer1M: 0.30)]
    K3,
}

internal static class KimiAiModelExtensions
{
    public static string ToApiModelId(this KimiAiModel model)
    {
        var member = typeof(KimiAiModel).GetField(model.ToString())
            ?? throw new ArgumentOutOfRangeException(nameof(model), model, "Unknown Kimi AI model.");

        var display = member.GetCustomAttribute<DisplayAttribute>()
            ?? throw new InvalidOperationException($"KimiAiModel.{model} is missing [Display(Name = \"...\")].");

        return display.Name
            ?? throw new InvalidOperationException($"KimiAiModel.{model} has empty Display.Name.");
    }
}
