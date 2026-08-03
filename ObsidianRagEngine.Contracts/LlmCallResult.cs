namespace ObsidianRagEngine.Contracts;

/// <summary>Token counts for a single LLM (or OCR) call.</summary>
public sealed record LlmTokenUsage(int InputTokens, int OutputTokens)
{
    public static LlmTokenUsage Zero { get; } = new(0, 0);

    public static LlmTokenUsage operator +(LlmTokenUsage left, LlmTokenUsage right) =>
        new(left.InputTokens + right.InputTokens, left.OutputTokens + right.OutputTokens);
}

/// <summary>Outcome of an LLM (or OCR) call: text, cost, and token usage.</summary>
public sealed record LlmCallResult(
    string Text,
    decimal Cost,
    LlmTokenUsage Usage)
{
    public LlmCallResult<T> ToTyped<T>(Func<string, T> convert) =>
        new(convert(Text), Cost, Usage);
}

/// <summary>Outcome of a typed JSON LLM call: value, cost, and token usage.</summary>
public sealed record LlmCallResult<T>(
    T Value,
    decimal Cost,
    LlmTokenUsage Usage);
