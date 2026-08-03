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
    Task<LlmCallResult> ExtractText(byte[] imageBytes, IReadOnlyList<OcrLanguage> languages, CancellationToken ct);
}
