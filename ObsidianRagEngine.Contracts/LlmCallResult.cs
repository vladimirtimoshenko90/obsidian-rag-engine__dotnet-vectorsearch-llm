namespace ObsidianRagEngine.Contracts;

/// <summary>Outcome of an LLM (or OCR) call: text, cost, and token usage.</summary>
public sealed record LlmCallResult(
    string Text,
    decimal Cost,
    int InputTokens,
    int OutputTokens);
