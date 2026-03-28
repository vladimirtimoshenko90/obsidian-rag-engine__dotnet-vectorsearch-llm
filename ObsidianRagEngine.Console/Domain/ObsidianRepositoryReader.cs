using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace ObsidianRagEngine.Console.Domain;

public interface IObsidianRepositoryReader
{
    List<ObsidianNoteInfo> IdentifyAllNotes();
    List<string> IdentifyAllImages();
    Task<ObsidianNoteFile> ReadNote(string filePath);
}

public class ObsidianRepositoryReader(string repositoryPath, string attachmentsFolder) : IObsidianRepositoryReader
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

    public List<string> IdentifyAllImages()
    {
        var attachmentsPath = Path.Combine(repositoryPath, attachmentsFolder);
        if (!Directory.Exists(attachmentsPath))
            return [];

        return Directory
            .EnumerateFiles(attachmentsPath, "*.*", SearchOption.TopDirectoryOnly)
            .Where(f =>
            {
                var ext = Path.GetExtension(f).ToLowerInvariant();
                return ext is ".png" or ".jpg" or ".jpeg" or ".gif" or ".webp" or ".svg" or ".bmp";
            })
            .Select(Path.GetFileName)
            .ToList()!;
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
            ContentHash = ComputeHash(content),
            Images = images
        };
    }

    private static string ComputeHash(string content)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(content));
        return Convert.ToHexStringLower(bytes);
    }
}
