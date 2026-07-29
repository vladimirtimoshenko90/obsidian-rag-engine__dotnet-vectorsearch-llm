namespace ObsidianRagEngine.Contracts;

public interface IOcrService
{
    string ModelName { get; }
    Task<string> ExtractText(byte[] imageBytes, IReadOnlyList<OcrLanguage> languages, CancellationToken ct);
}
