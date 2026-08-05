namespace ObsidianRagEngine.Contracts;

/// <summary>
/// Shared OCR languages for <see cref="IOcrProvider"/> and vision LLM prompts.
/// </summary>
public enum OcrLanguage
{
    English,
    Russian
}

public interface IOcrProvider
{
    string ModelName { get; }

    /// <param name="clarificationPrompt">
    /// Optional domain hint sent as an extra vision-OCR message (ignored by backends with no prompt channel).
    /// </param>
    Task<LlmCallResult> ExtractText(
        byte[] imageBytes,
        IReadOnlyList<OcrLanguage> languages,
        CancellationToken ct,
        string? clarificationPrompt = null);
}
