namespace ObsidianRagEngine.Contracts;

/// <summary>
/// Shared OCR languages for <see cref="IOcrProvider"/> and vision LLM prompts.
/// </summary>
public enum OcrLanguage
{
    English,
    Russian
}

/// <summary>
/// Extracts text from images (classic OCR and/or vision LLM backends).
/// </summary>
public interface IOcrProvider
{
    string ModelName { get; }

    /// <param name="languages">
    /// Hint for the OCR engine under the hood. The first entry is also used for prompt localization
    /// (falls back to <see cref="OcrLanguage.English"/> when the list is empty).
    /// </param>
    /// <param name="clarificationPrompt">
    /// Optional domain hint sent as an extra vision-OCR message (ignored by backends with no prompt channel).
    /// </param>
    Task<LlmCallResult> ExtractText(
        byte[] imageBytes,
        IReadOnlyList<OcrLanguage> languages,
        CancellationToken ct,
        string? clarificationPrompt = null);
}
