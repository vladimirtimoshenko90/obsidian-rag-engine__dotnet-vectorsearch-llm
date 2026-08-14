using ObsidianRagEngine.Console.Data.ObsidianNotes.Entities;
using ObsidianRagEngine.Console.Data.ObsidianNotes.Repositories;
using ObsidianRagEngine.Console.Domain.ObsidianVault;
using ObsidianRagEngine.Contracts;
using System.Text.RegularExpressions;

namespace ObsidianRagEngine.Console.Domain.ObsidianNoteIngestion.Sanitization;

public interface IObsidianNoteSanitizationService
{
    Task<string> Sanitize(NoteFileData noteFile, CancellationToken ct);
}

public class ObsidianNoteSanitizationService(
    IObsidianImageRepository noteImageRepo,
    IOcrProvider ocr) : IObsidianNoteSanitizationService
{
    public async Task<string> Sanitize(NoteFileData noteFile, CancellationToken ct)
    {
        var sanitizedText = noteFile.Content;

        foreach (var imagePath in noteFile.ImagePaths)
        {
            var ocrResult = await noteImageRepo.GetByFilePathAndOcrModel(imagePath, ocr.ModelName, ct);
            if (ocrResult is null)
            {
                var imageBytes = await File.ReadAllBytesAsync(imagePath, ct);
                var extractResult = await ocr.ExtractText(
                    imageBytes,
                    [OcrLanguage.Russian, OcrLanguage.English],
                    ct);

                ocrResult = await noteImageRepo.Create(new ObsidianImage
                {
                    FilePath = imagePath,
                    OcrModel = ocr.ModelName,
                    ExtractedText = extractResult.Text
                }, ct);
            }

            var imageEmbed = $"![[{Path.GetFileName(imagePath)}]]";
            sanitizedText = sanitizedText.Replace(imageEmbed, ocrResult.ExtractedText);
        }

        sanitizedText = Regex.Replace(sanitizedText, @"#(topic|root)(/\w+)*", "");  // removing tags
        sanitizedText = Regex.Replace(sanitizedText, @"\[\[.*?\]\]", "");   // removing links
        sanitizedText = sanitizedText.Trim();   // trim, just trim

        return sanitizedText;
    }
}
