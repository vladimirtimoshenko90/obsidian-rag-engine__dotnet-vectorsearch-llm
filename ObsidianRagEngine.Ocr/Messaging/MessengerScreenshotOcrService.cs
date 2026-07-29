using ObsidianRagEngine.Contracts;

namespace ObsidianRagEngine.Ocr.Messaging;

/// <summary>
/// Messenger-screenshot OCR pipeline: split panels → normalize → per-panel OCR → LLM merge/cleanup.
/// Pass optional <see cref="MessengerOcrCallbacks"/> per <see cref="ExtractText"/> call for intermediate artifacts.
/// Depends on OCR and LLM abstractions only — backends are injected at composition root.
/// </summary>
public sealed class MessengerScreenshotOcrService(
    IOcrService ocr,
    ILlmService llm,
    MessengerPanelSplitter splitter,
    MessengerPanelNormalizer normalizer) : IOcrService
{
    // Distinct from raw OCR cache keys: "{base}-messenger".
    public string ModelName => $"{ocr.ModelName}-messenger";

    public Task<string> ExtractText(byte[] imageBytes, IReadOnlyList<string> languages, CancellationToken ct) =>
        ExtractText(imageBytes, languages, ct, callbacks: null);

    public async Task<string> ExtractText(
        byte[] imageBytes,
        IReadOnlyList<string> languages,
        CancellationToken ct,
        MessengerOcrCallbacks? callbacks)
    {
        // Side-by-side composites become one crop per phone panel (or the whole image if no seams).
        var panels = splitter.Split(imageBytes);

        var texts = new List<string>(panels.Count);
        foreach (var panel in panels)
        {
            // Dark UI / colored bubbles → dark text on a light background for Tesseract.
            var normalized = normalizer.Normalize(panel);
            var text = await ocr.ExtractText(normalized, languages, ct);
            callbacks?.OnPanelOcr?.Invoke(panel, normalized, text);
            texts.Add(text);
        }

        // Strip chrome, drop panel overlap duplicates, keep message timestamps; always run (even for one panel).
        var promptMerge = MessengerTranscriptPromptBuilder.BuildPrompt(texts);
        var transcript = await llm.Generate(promptMerge, ct);

        return transcript.Trim();
    }
}
