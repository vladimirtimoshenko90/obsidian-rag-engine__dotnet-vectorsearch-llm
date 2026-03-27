namespace ObsidianRagEngine.Console.Data.ObsidianNoteChunks.Entities;

public class ObsidianNoteChunk
{
    public Guid Id { get; set; }
    public int NoteId { get; set; }
    public string Text { get; set; } = string.Empty;
    public float[] Embedding { get; set; } = [];
}
