using ObsidianRagEngine.Contracts;
using ObsidianRagEngine.Llm.Pricing;
using OpenAI.Chat;

namespace ObsidianRagEngine.Llm;

internal static class LlmCallResultFactory
{
    public static LlmCallResult FromChatCompletion(ChatCompletion completion, Enum model)
    {
        var text = completion.Content is { Count: > 0 }
            ? completion.Content[0].Text ?? string.Empty
            : string.Empty;
        var usage = completion.Usage;

        return new LlmCallResult(
            text,
            LlmCostCalculator.Cost(model, usage),
            usage?.InputTokenCount ?? 0,
            usage?.OutputTokenCount ?? 0);
    }
}
