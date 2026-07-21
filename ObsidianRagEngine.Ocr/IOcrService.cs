namespace ObsidianRagEngine.Ocr;

public interface IOcrService
{
    string ModelName { get; }
    Task<string> ExtractText(byte[] imageBytes, IReadOnlyList<string> languages, CancellationToken ct);
}
