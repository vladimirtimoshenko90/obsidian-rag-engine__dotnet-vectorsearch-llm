using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace ObsidianRagEngine.Llm.OpenRouter;

// OpenRouter model slugs — https://openrouter.ai/models
public enum OpenRouterAiModel
{
    [Display(Name = "anthropic/claude-fable-5")]
    ClaudeFable5,

    [Display(Name = "anthropic/claude-opus-5")]
    ClaudeOpus5,

    [Display(Name = "anthropic/claude-sonnet-5")]
    ClaudeSonnet5,

    [Display(Name = "openai/gpt-5.6-sol")]
    Gpt56Sol,

    [Display(Name = "openai/gpt-5.6-terra")]
    Gpt56Terra,

    [Display(Name = "openai/gpt-5.6-luna")]
    Gpt56Luna,

    [Display(Name = "deepseek/deepseek-v4-pro")]
    DeepSeekV4Pro,

    [Display(Name = "deepseek/deepseek-v4-flash")]
    DeepSeekV4Flash,
}

internal static class OpenRouterAiModelExtensions
{
    public static string ToApiModelId(this OpenRouterAiModel model)
    {
        var member = typeof(OpenRouterAiModel).GetField(model.ToString())
            ?? throw new ArgumentOutOfRangeException(nameof(model), model, "Unknown OpenRouter AI model.");

        var display = member.GetCustomAttribute<DisplayAttribute>()
            ?? throw new InvalidOperationException($"OpenRouterAiModel.{model} is missing [Display(Name = \"...\")].");

        return display.Name
            ?? throw new InvalidOperationException($"OpenRouterAiModel.{model} has empty Display.Name.");
    }
}
