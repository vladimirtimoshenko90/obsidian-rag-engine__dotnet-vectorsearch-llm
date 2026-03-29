using ObsidianRagEngine.Console.Data.ObsidianNotes.Entities;
using ObsidianRagEngine.Console.Data.ObsidianNotes.Repositories;

namespace ObsidianRagEngine.Console.Domain;

public interface IObsidianNoteProcessingService
{
    Task ProcessNote(ObsidianNoteInfo noteInfo, CancellationToken ct = default);
}

public class ObsidianNoteProcessingService(
    IObsidianRepositoryReader obsidianRepo,
    IObsidianNoteRepository noteRepo,
    IObsidianImageRepository noteImageRepo,
    IImageOcrService ocrService) : IObsidianNoteProcessingService
{
    private const string OcrModel = "tesseract";

    public async Task ProcessNote(ObsidianNoteInfo noteInfo, CancellationToken ct = default)
    {
        var noteFile = await obsidianRepo.ReadNote(noteInfo.FilePath);

        var existingNote = await noteRepo.GetByFilePath(noteInfo.FilePath, ct);

        if (existingNote is not null)
        {
            if (existingNote.ContentHash == noteFile.ContentHash)
                return;

            await noteImageRepo.DeleteByNoteId(existingNote.Id, ct);
            await noteRepo.Delete(existingNote.Id, ct);
        }

        var newNote = await noteRepo.Create(new ObsidianNote
        {
            FilePath = noteFile.FilePath,
            ContentHash = noteFile.ContentHash,
            Text = noteFile.Content
        }, ct);

        foreach (var imagePath in noteFile.ImagePaths)
        {
            var existing = await noteImageRepo.GetByFilePathAndOcrModel(imagePath, OcrModel, ct);
            if (existing is not null)
                continue;

            var imageBytes = await File.ReadAllBytesAsync(imagePath, ct);
            var extractedText = await ocrService.ExtractText(imageBytes);

            await noteImageRepo.Create(new ObsidianImage
            {
                NoteId = newNote.Id,
                FilePath = imagePath,
                OcrModel = OcrModel,
                ExtractedText = extractedText
            }, ct);
        }
    }
}