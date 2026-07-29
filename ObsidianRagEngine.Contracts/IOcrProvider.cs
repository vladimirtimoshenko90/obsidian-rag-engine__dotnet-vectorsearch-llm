namespace ObsidianRagEngine.Contracts;

public interface IOcrProvider
{
    string ModelName { get; }
    Task<string> ExtractText(byte[] imageBytes, IReadOnlyList<OcrLanguage> languages, CancellationToken ct);
}
