using ObsidianRagEngine.Contracts;
using ObsidianRagEngine.Ocr.Pipelines.Messenger.Merging;
using ObsidianRagEngine.Ocr.Pipelines.Messenger.Normalization;
using ObsidianRagEngine.Ocr.Pipelines.Messenger.Splitting;

namespace ObsidianRagEngine.Ocr.Pipelines.Messenger;

/// <summary>
/// Messenger-screenshot OCR pipeline: split panels → normalize → per-panel OCR → LLM merge/cleanup.
/// Pass optional <see cref="MessengerOcrCallbacks"/> per <see cref="ExtractText"/> call for intermediate artifacts.
/// Depends on OCR and LLM abstractions only — backends are injected at composition root.
/// </summary>
public sealed class MessengerOcrService(IOcrProvider ocr, ILlmProvider llm) : IOcrProvider
{
    private readonly MessengerPanelSplitter _splitter = new();
    private readonly MessengerPanelNormalizer _normalizer = new();

    public string ModelName => $"messenger__{ocr.ModelName}__{llm.ModelName}";

    public Task<LlmCallResult> ExtractText(
        byte[] imageBytes,
        IReadOnlyList<OcrLanguage> languages,
        CancellationToken ct,
        string? clarificationPrompt = null) =>
        ExtractText(imageBytes, languages, ct, callbacks: null, clarificationPrompt);

    public async Task<LlmCallResult> ExtractText(
        byte[] imageBytes,
        IReadOnlyList<OcrLanguage> languages,
        CancellationToken ct,
        MessengerOcrCallbacks? callbacks,
        string? clarificationPrompt = null)
    {
        var metrics = new CallMetricsTotals();

        // Side-by-side composites become one crop per phone panel (or the whole image if no seams).
        var panels = _splitter.Split(imageBytes);

        var texts = new List<string>(panels.Count);
        foreach (var panel in panels)
        {
            // Dark UI / colored bubbles → dark text on a light background for Tesseract.
            var normalized = _normalizer.Normalize(panel);

            var ocrResult = await ocr.ExtractText(normalized, languages, ct, clarificationPrompt);
            callbacks?.OnPanelOcr?.Invoke(panel, normalized, ocrResult.Text);

            texts.Add(ocrResult.Text);
            metrics.Add(ocrResult);
        }

        // Strip chrome, drop panel overlap duplicates, keep message timestamps; always run (even for one panel).
        var promptMerge = MessengerTranscriptPromptBuilder.BuildPrompt(texts);
        var mergeResult = await llm.Complete(promptMerge, ct);

        metrics.Add(mergeResult);

        return new LlmCallResult(mergeResult.Text.Trim(), metrics.Cost, metrics.Usage);
    }

    private sealed class CallMetricsTotals
    {
        public decimal Cost { get; private set; }
        public LlmTokenUsage Usage { get; private set; } = LlmTokenUsage.Zero;

        public void Add(LlmCallResult res)
        {
            Cost += res.Cost;
            Usage += res.Usage;
        }
    }
}

/// <summary>
/// Optional per-run intermediate-result callbacks for <see cref="MessengerOcrService.ExtractText"/>.
/// Leave properties null to skip a hook.
/// </summary>
public sealed class MessengerOcrCallbacks
{
    /// <summary>Called after each panel is normalized and OCR'd (raw crop, normalized image, text).</summary>
    public Action<byte[], byte[], string>? OnPanelOcr { get; init; }
}
