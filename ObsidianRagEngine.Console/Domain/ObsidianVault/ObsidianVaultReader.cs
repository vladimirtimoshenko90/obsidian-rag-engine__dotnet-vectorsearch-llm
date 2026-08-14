using ObsidianRagEngine.Console.Common.Utility;
using Microsoft.Extensions.Options;
using System.Text.RegularExpressions;

namespace ObsidianRagEngine.Console.Domain.ObsidianVault;

public interface IObsidianVaultReader
{
    List<NoteFileInfo> IdentifyAllNotes();
    List<NoteFileInfo> IdentifyAllImages();
    Task<NoteFileData> ReadNote(string filePath);
}

public class ObsidianVaultReader(IOptions<ObsidianVaultSettings> settings) : IObsidianVaultReader
{
    private static readonly Regex ImagePattern =
        new(@"!\[\[([^\]]+\.(?:png|jpg|jpeg|gif|webp|svg|bmp))\]\]",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public List<NoteFileInfo> IdentifyAllNotes()
    {
        return Directory
            .EnumerateFiles(settings.Value.Path, "*.md", SearchOption.AllDirectories)
            .Select(filePath => new NoteFileInfo
            {
                FileName = Path.GetFileNameWithoutExtension(filePath),
                FilePath = filePath
            })
            .ToList();
    }

    public List<NoteFileInfo> IdentifyAllImages()
    {
        var attachmentsPath = Path.Combine(settings.Value.Path, settings.Value.AttachmentsFolder);
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
        var attachmentsPath = Path.Combine(settings.Value.Path, settings.Value.AttachmentsFolder);

        var content = await File.ReadAllTextAsync(filePath);

        var imagePaths = ImagePattern.Matches(content)
            .Select(m => m.Groups[1].Value)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Select(imageFileName => Path.Combine(attachmentsPath, Path.GetFileName(imageFileName)))
            .Where(File.Exists)
            .ToList();

        return new NoteFileData
        {
            FileName = Path.GetFileNameWithoutExtension(filePath),
            FilePath = filePath,
            Content = content,
            ContentHash = HashUtility.ComputeHash(content),
            ImagePaths = imagePaths
        };
    }
}

public sealed class ObsidianVaultSettings
{
    public string Path { get; set; } = string.Empty;
    public string AttachmentsFolder { get; set; } = string.Empty;
}
