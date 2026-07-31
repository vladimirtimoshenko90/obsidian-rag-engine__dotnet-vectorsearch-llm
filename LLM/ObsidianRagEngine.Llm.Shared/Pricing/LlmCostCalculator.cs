using System.Reflection;
using OpenAI.Chat;

namespace ObsidianRagEngine.Llm.Pricing;

/// <summary>Estimates cost from token usage and a model tariff.</summary>
public static class LlmCostCalculator
{
    private const decimal TokensPerMillion = 1_000_000m;

    public static decimal Cost(Enum model, ChatTokenUsage? usage)
    {
        if (usage is null)
            return 0m;

        var tariff = TariffFor(model);
        var inputTokens = usage.InputTokenCount;
        var outputTokens = usage.OutputTokenCount;
        var cachedInputTokens = Math.Min(usage.InputTokenDetails?.CachedTokenCount ?? 0, inputTokens);
        var missTokens = inputTokens - cachedInputTokens;
        var cachedRate = tariff.CachedInputPer1M ?? tariff.InputPer1M;

        return missTokens / TokensPerMillion * tariff.InputPer1M
             + cachedInputTokens / TokensPerMillion * cachedRate
             + outputTokens / TokensPerMillion * tariff.OutputPer1M;
    }

    private static LlmTariffAttribute TariffFor(Enum model)
    {
        var field = model.GetType().GetField(model.ToString())
            ?? throw new ArgumentOutOfRangeException(nameof(model), model, "Unknown model enum value.");

        return field.GetCustomAttribute<LlmTariffAttribute>()
            ?? throw new InvalidOperationException($"{model.GetType().Name}.{model} is missing [LlmTariff].");
    }
}
