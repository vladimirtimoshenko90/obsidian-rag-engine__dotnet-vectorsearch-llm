namespace ObsidianRagEngine.Contracts;

public interface IOcrProvider
{
    string ModelName { get; }
    Task<LlmCallResult> ExtractText(byte[] imageBytes, IReadOnlyList<OcrLanguage> languages, CancellationToken ct);
}
