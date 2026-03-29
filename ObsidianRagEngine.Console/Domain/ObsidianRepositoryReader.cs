using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace ObsidianRagEngine.Console.Domain;

public interface IObsidianRepositoryReader
{
    List<NoteFileInfo> IdentifyAllNotes();
    List<NoteFileInfo> IdentifyAllImages();
    Task<NoteFileData> ReadNote(string filePath);
}

public class ObsidianRepositoryReader(string repositoryPath, string attachmentsFolder) : IObsidianRepositoryReader
{
    private static readonly Regex ImagePattern =
        new(@"!\[\[([^\]]+\.(?:png|jpg|jpeg|gif|webp|svg|bmp))\]\]",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public List<NoteFileInfo> IdentifyAllNotes()
    {
        return Directory
            .EnumerateFiles(repositoryPath, "*.md", SearchOption.AllDirectories)
            .Select(filePath => new NoteFileInfo
            {
                FileName = Path.GetFileNameWithoutExtension(filePath),
                FilePath = filePath
            })
            .ToList();
    }

    public List<NoteFileInfo> IdentifyAllImages()
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
            .Select(f => new NoteFileInfo
            {
                FileName = Path.GetFileNameWithoutExtension(f),
                FilePath = f
            })
            .ToList();
    }

    public async Task<NoteFileData> ReadNote(string filePath)
    {
        var content = await File.ReadAllTextAsync(filePath);

        var imagePaths = ImagePattern.Matches(content)
            .Select(m => m.Groups[1].Value)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Select(ResolveImagePath)
            .Where(path => path is not null)
            .ToList()!;

        return new NoteFileData
        {
            FileName = Path.GetFileNameWithoutExtension(filePath),
            FilePath = filePath,
            Content = content,
            ContentHash = ComputeHash(content),
            ImagePaths = imagePaths!
        };
    }

    private string? ResolveImagePath(string imageFileName)
    {
        return Directory
            .EnumerateFiles(repositoryPath, imageFileName, SearchOption.AllDirectories)
            .FirstOrDefault();
    }

    private static string ComputeHash(string content)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(content));
        return Convert.ToHexStringLower(bytes);
    }
}

public class NoteFileInfo
{
    public required string FileName { get; init; }
    public required string FilePath { get; init; }
}

public class NoteFileData
{
    public required string FileName { get; init; }
    public required string FilePath { get; init; }
    public required string Content { get; init; }
    public required string ContentHash { get; init; }
    public required IReadOnlyList<string> ImagePaths { get; init; }
}