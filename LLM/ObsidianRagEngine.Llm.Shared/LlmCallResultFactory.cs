using ObsidianRagEngine.Contracts;
using ObsidianRagEngine.Llm.Pricing;
using OpenAI.Chat;

namespace ObsidianRagEngine.Llm;

internal static class LlmCallResultFactory
{
    public static LlmCallResult FromChatCompletion(ChatCompletion completion, Enum model)
    {
        var (text, usage, tokenUsage) = Unpack(completion);
        return new LlmCallResult(text, LlmCostCalculator.Cost(model, usage), tokenUsage);
    }

    /// <summary>
    /// OpenRouter returns the billed amount in <c>usage.cost</c> (not modeled on <see cref="ChatTokenUsage"/>).
    /// </summary>
    public static LlmCallResult FromOpenRouterChatCompletion(ChatCompletion completion)
    {
        var (text, _, tokenUsage) = Unpack(completion);

#pragma warning disable SCME0001 // JsonPatch is evaluation-only in System.ClientModel
        var cost = completion.Patch.TryGetValue("$.usage.cost"u8, out decimal openRouterCost)
            ? openRouterCost
            : 0m;
#pragma warning restore SCME0001

        return new LlmCallResult(text, cost, tokenUsage);
    }

    private static (string Text, ChatTokenUsage? Usage, LlmTokenUsage TokenUsage) Unpack(ChatCompletion completion)
    {
        var text = completion.Content is { Count: > 0 }
            ? completion.Content[0].Text ?? string.Empty
            : string.Empty;

        var usage = completion.Usage;
        var tokenUsage = usage is not null
            ? new LlmTokenUsage(usage.InputTokenCount, usage.OutputTokenCount)
            : LlmTokenUsage.Zero;

        return (text, usage, tokenUsage);
    }
}
