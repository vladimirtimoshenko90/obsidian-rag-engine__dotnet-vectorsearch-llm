namespace ObsidianRagEngine.Console.Data.ObsidianNotes.Entities;

public class ObsidianImage
{
    public int Id { get; set; }
    public string FilePath { get; set; } = string.Empty;
    public string OcrModel { get; set; } = string.Empty;
    public string ExtractedText { get; set; } = string.Empty;
}
