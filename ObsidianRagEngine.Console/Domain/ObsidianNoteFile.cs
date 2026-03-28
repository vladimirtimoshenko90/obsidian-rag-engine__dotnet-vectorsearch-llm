namespace ObsidianRagEngine.Console.Domain;

public class ObsidianNoteFile
{
    public string FilePath { get; init; } = string.Empty;
    public string Content { get; init; } = string.Empty;
    public string ContentHash { get; init; } = string.Empty;
    public IReadOnlyList<string> Images { get; init; } = [];
}
