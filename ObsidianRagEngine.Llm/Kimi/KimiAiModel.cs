using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace ObsidianRagEngine.Llm.Kimi;

public enum KimiAiModel
{
    /// <summary>Cheapest among current multimodal; best general / average use. Thinking always off.</summary>
    [Display(Name = "kimi-k2.6")]
    K2_6,

    /// <summary>Fastest output (~180–260 tok/s). Coding-focused; thinking always on.</summary>
    [Display(Name = "kimi-k2.7-code-highspeed")]
    K2_7_CodeHighspeed,

    /// <summary>Smartest / flagship. Thinking always on (reasoning_effort max).</summary>
    [Display(Name = "kimi-k3")]
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
