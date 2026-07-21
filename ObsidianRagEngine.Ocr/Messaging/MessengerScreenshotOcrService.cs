namespace ObsidianRagEngine.Ocr.Messaging;

/// <summary>
/// Messenger-screenshot OCR pipeline: split panels → normalize → per-panel OCR → LLM merge/cleanup.
/// </summary>
public sealed class MessengerScreenshotOcrService(
    IOcrService ocr,
    MessengerPanelSplitter splitter,
    MessengerPanelNormalizer normalizer,
    IMessengerTranscriptMerger merger) : IOcrService
{
    // Distinct from raw "tesseract" so cached OCR rows are not reused after the pipeline change.
    public string ModelName => "tesseract-messenger";

    public async Task<string> ExtractText(byte[] imageBytes, IReadOnlyList<string> languages)
    {
        // Side-by-side composites become one crop per phone panel (or the whole image if no seams).
        var panels = splitter.Split(imageBytes);

        var texts = new List<string>(panels.Count);
        foreach (var panel in panels)
        {
            // Dark UI / colored bubbles → dark text on a light background for Tesseract.
            var normalized = normalizer.Normalize(panel);
            texts.Add(await ocr.ExtractText(normalized, languages));
        }

        // Strip chrome, drop panel overlap duplicates, keep message timestamps; always run (even for one panel).
        var transcript = await merger.MergeAsync(texts);
        return transcript;
    }
}
