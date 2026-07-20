namespace ObsidianRagEngine.Console.Domain.Reading;

public class NoteFileData
{
    public required string FileName { get; init; }
    public required string FilePath { get; init; }
    public required string Content { get; init; }
    public required string ContentHash { get; init; }
    public required IReadOnlyList<string> ImagePaths { get; init; }
}
