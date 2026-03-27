using System.Text.RegularExpressions;

namespace ObsidianRagEngine.Console.Domain;

public interface IObsidianRepositoryReader
{
    List<ObsidianNoteInfo> IdentifyAllNotes();
    Task<ObsidianNoteFile> ReadNote(string filePath);
}

public class ObsidianRepositoryReader(string repositoryPath) : IObsidianRepositoryReader
{
    private static readonly Regex ImagePattern =
        new(@"!\[\[([^\]]+\.(?:png|jpg|jpeg|gif|webp|svg|bmp))\]\]",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public List<ObsidianNoteInfo> IdentifyAllNotes()
    {
        return Directory
            .EnumerateFiles(repositoryPath, "*.md", SearchOption.AllDirectories)
            .Select(filePath => new ObsidianNoteInfo
            {
                Name = Path.GetFileNameWithoutExtension(filePath),
                FilePath = filePath
            })
            .ToList();
    }

    public async Task<ObsidianNoteFile> ReadNote(string filePath)
    {
        var content = await File.ReadAllTextAsync(filePath);

        var images = ImagePattern.Matches(content)
            .Select(m => m.Groups[1].Value)
            .ToList();

        return new ObsidianNoteFile
        {
            FilePath = filePath,
            Content = content,
            Images = images
        };
    }
}
