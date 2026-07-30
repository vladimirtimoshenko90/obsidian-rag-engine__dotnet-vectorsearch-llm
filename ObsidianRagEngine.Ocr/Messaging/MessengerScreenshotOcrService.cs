using ObsidianRagEngine.Contracts;
using ObsidianRagEngine.Ocr.Messaging.Merging;
using ObsidianRagEngine.Ocr.Messaging.Normalization;
using ObsidianRagEngine.Ocr.Messaging.Splitting;

namespace ObsidianRagEngine.Ocr.Messaging;

/// <summary>
/// Messenger-screenshot OCR pipeline: split panels → normalize → per-panel OCR → LLM merge/cleanup.
/// Pass optional <see cref="MessengerOcrCallbacks"/> per <see cref="ExtractText"/> call for intermediate artifacts.
/// Depends on OCR and LLM abstractions only — backends are injected at composition root.
/// </summary>
public sealed class MessengerScreenshotOcrService(IOcrProvider ocr, ILlmProvider llm) : IOcrProvider
{
    private readonly MessengerPanelSplitter _splitter = new();
    private readonly MessengerPanelNormalizer _normalizer = new();

    // Distinct from raw OCR cache keys: "{ocr}+{llm}-messenger".
    public string ModelName => $"{ocr.ModelName}+{llm.ModelName}-messenger";

    public Task<string> ExtractText(byte[] imageBytes, IReadOnlyList<OcrLanguage> languages, CancellationToken ct) =>
        ExtractText(imageBytes, languages, ct, callbacks: null);

    public async Task<string> ExtractText(
        byte[] imageBytes,
        IReadOnlyList<OcrLanguage> languages,
        CancellationToken ct,
        MessengerOcrCallbacks? callbacks)
    {
        // Side-by-side composites become one crop per phone panel (or the whole image if no seams).
        var panels = _splitter.Split(imageBytes);

        var texts = new List<string>(panels.Count);
        foreach (var panel in panels)
        {
            // Dark UI / colored bubbles → dark text on a light background for Tesseract.
            var normalized = _normalizer.Normalize(panel);
            var text = await ocr.ExtractText(normalized, languages, ct);
            callbacks?.OnPanelOcr?.Invoke(panel, normalized, text);
            texts.Add(text);
        }

        // Strip chrome, drop panel overlap duplicates, keep message timestamps; always run (even for one panel).
        var promptMerge = MessengerTranscriptPromptBuilder.BuildPrompt(texts);
        var transcript = await llm.Complete(promptMerge, ct);

        return transcript.Trim();
    }
}

/// <summary>
/// Optional per-run intermediate-result callbacks for <see cref="MessengerScreenshotOcrService.ExtractText"/>.
/// Leave properties null to skip a hook.
/// </summary>
public sealed class MessengerOcrCallbacks
{
    /// <summary>Called after each panel is normalized and OCR'd (raw crop, normalized image, text).</summary>
    public Action<byte[], byte[], string>? OnPanelOcr { get; init; }
}
