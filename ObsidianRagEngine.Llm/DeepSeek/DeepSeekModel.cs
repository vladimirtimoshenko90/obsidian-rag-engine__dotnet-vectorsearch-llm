using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace ObsidianRagEngine.Llm.DeepSeek;

public enum DeepSeekModel
{
    [Display(Name = "deepseek-v4-flash")]
    Flash,

    [Display(Name = "deepseek-v4-pro")]
    Pro,
}

internal static class DeepSeekModelExtensions
{
    public static string ToApiModelId(this DeepSeekModel model)
    {
        var member = typeof(DeepSeekModel).GetField(model.ToString())
            ?? throw new ArgumentOutOfRangeException(nameof(model), model, "Unknown DeepSeek model.");

        var display = member.GetCustomAttribute<DisplayAttribute>()
            ?? throw new InvalidOperationException($"DeepSeekModel.{model} is missing [Display(Name = \"...\")].");

        return display.Name
            ?? throw new InvalidOperationException($"DeepSeekModel.{model} has empty Display.Name.");
    }
}
