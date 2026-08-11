using ObsidianRagEngine.Console.Data.ObsidianNotes.Entities;
using ObsidianRagEngine.Console.Data.ObsidianNotes.Repositories;
using ObsidianRagEngine.Console.Domain.Reading;
using ObsidianRagEngine.Console.Domain.Vectorization;
using ObsidianRagEngine.Contracts;
using System.Text.RegularExpressions;

namespace ObsidianRagEngine.Console.Domain.Ingestion;

public interface IObsidianNoteIngestionService
{
    Task ProcessNote(NoteFileData noteFile, CancellationToken ct = default);
}

public class ObsidianNoteIngestionService(
    IObsidianNoteRepository noteRepo,
    IObsidianImageRepository noteImageRepo,
    IOcrProvider ocr,
    IObsidianNoteVectorizationService vectorizationService) : IObsidianNoteIngestionService
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

        var note = await noteRepo.Create(new ObsidianNote
        {
            FilePath = noteFile.FilePath,
            ContentHash = noteFile.ContentHash,
            TextRaw = noteFile.Content,
            TextSanitized = sanitizedText
        }, ct);

        await vectorizationService.VectorizeNote(note, ct);
    }
}
