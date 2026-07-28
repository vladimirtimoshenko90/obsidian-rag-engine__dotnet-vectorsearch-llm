using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace ObsidianRagEngine.Llm.Alibaba;

public enum AlibabaAiModel
{
    /// <summary>Best average; default chat/JSON/Generate; vision OCR default.</summary>
    [Display(Name = "qwen3.7-plus-2026-05-26")]
    Qwen37Plus,

    /// <summary>Smartest / flagship text.</summary>
    [Display(Name = "qwen3.7-max-2026-06-08")]
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
