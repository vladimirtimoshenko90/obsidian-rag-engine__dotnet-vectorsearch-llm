namespace ObsidianRagEngine.Console.Data.ObsidianNotes.Entities;

public class ObsidianNote
{
    public int Id { get; set; }
    public string FilePath { get; set; } = string.Empty;
    public string ContentHash { get; set; } = string.Empty;
    public string TextRaw { get; set; } = string.Empty;
    public string TextSanitized { get; set; } = string.Empty;
    public string? OcrModel { get; set; }
    public string? EmbeddingModel { get; set; }
}
