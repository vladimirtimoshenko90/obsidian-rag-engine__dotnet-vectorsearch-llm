using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace ObsidianRagEngine.Llm.DeepSeek;

public enum DeepSeekAiModel
{
    [Display(Name = "deepseek-v4-flash")]
    Flash,

    [Display(Name = "deepseek-v4-pro")]
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
