using System.ComponentModel.DataAnnotations;
using System.Reflection;
using ObsidianRagEngine.Llm.Pricing;

namespace ObsidianRagEngine.Llm.DeepSeek;

// [LlmTariff] rates: per 1M tokens (input / output / cached input).
// Pricing: https://api-docs.deepseek.com/quick_start/pricing/
public enum DeepSeekAiModel
{
    [Display(Name = "deepseek-v4-flash")]
    [LlmTariff(0.14, 0.28, cachedInputPer1M: 0.0028)]
    Flash,

    [Display(Name = "deepseek-v4-pro")]
    [LlmTariff(0.435, 0.87, cachedInputPer1M: 0.003625)]
    Pro,
}

internal static class DeepSeekAiModelExtensions
{
    public static string ToApiModelId(this DeepSeekAiModel model)
    {
        var member = typeof(DeepSeekAiModel).GetField(model.ToString())
            ?? throw new ArgumentOutOfRangeException(nameof(model), model, "Unknown DeepSeek AI model.");

        var display = member.GetCustomAttribute<DisplayAttribute>()
            ?? throw new InvalidOperationException($"DeepSeekAiModel.{model} is missing [Display(Name = \"...\")].");

        return display.Name
            ?? throw new InvalidOperationException($"DeepSeekAiModel.{model} has empty Display.Name.");
    }
}
