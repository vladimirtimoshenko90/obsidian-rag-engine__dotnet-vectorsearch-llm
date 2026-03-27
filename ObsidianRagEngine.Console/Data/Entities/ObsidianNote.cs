namespace ObsidianRagEngine.Console.Data.Entities;

public class ObsidianNote
{
    public int Id { get; set; }
    public string FilePath { get; set; } = string.Empty;
    public string ContentHash { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
    public string? OcrModel { get; set; }
    public string? EmbeddingModel { get; set; }
}
