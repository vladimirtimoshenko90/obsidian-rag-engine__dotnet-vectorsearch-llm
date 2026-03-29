using ObsidianRagEngine.Console.Data.ObsidianNotes.Entities;
using ObsidianRagEngine.Console.Data.ObsidianNotes.Repositories;
using System.Text.RegularExpressions;

namespace ObsidianRagEngine.Console.Domain;

public interface IObsidianNoteProcessingService
{
    Task ProcessNote(NoteFileData noteFile, CancellationToken ct = default);
}

public class ObsidianNoteProcessingService(
    IObsidianNoteRepository noteRepo,
    IObsidianImageRepository noteImageRepo,
    IImageOcrService ocrService) : IObsidianNoteProcessingService
{
    public async Task ProcessNote(NoteFileData noteFile, CancellationToken ct = default)
    {
        var existingNote = await noteRepo.GetByFilePath(noteFile.FilePath, ct);

        if (existingNote is not null)
        {
            if (existingNote.ContentHash == noteFile.ContentHash)
                return;

            await noteRepo.Delete(existingNote.Id, ct);
        }

        var sanitizedText = noteFile.Content;

        foreach (var imagePath in noteFile.ImagePaths)
        {
            var ocrResult = await noteImageRepo.GetByFilePathAndOcrModel(imagePath, ocrService.ModelName, ct);
            if (ocrResult is null)
            {
                var imageBytes = await File.ReadAllBytesAsync(imagePath, ct);
                var extractedText = await ocrService.ExtractText(imageBytes);

                ocrResult = await noteImageRepo.Create(new ObsidianImage
                {
                    FilePath = imagePath,
                    OcrModel = ocrService.ModelName,
                    ExtractedText = extractedText
                }, ct);
            }

            var imageEmbed = $"![[{Path.GetFileName(imagePath)}]]";
            sanitizedText = sanitizedText.Replace(imageEmbed, ocrResult.ExtractedText);
        }

        sanitizedText = Regex.Replace(sanitizedText, @"#(topic|root)(/\w+)*", "");  // removing tags
        sanitizedText = Regex.Replace(sanitizedText, @"\[\[.*?\]\]", "");   // removing links
        sanitizedText = sanitizedText.Trim();   // trim, just trim

        await noteRepo.Create(new ObsidianNote
        {
            FilePath = noteFile.FilePath,
            ContentHash = noteFile.ContentHash,
            TextRaw = noteFile.Content,
            TextSanitized = sanitizedText
        }, ct);
    }
}