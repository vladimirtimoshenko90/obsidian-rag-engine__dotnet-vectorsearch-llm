namespace ObsidianRagEngine.Ocr.Messaging;

/// <summary>
/// Optional per-run intermediate-result callbacks for <see cref="MessengerScreenshotOcrService.ExtractText"/>.
/// Leave properties null to skip a hook.
/// </summary>
public sealed class MessengerOcrCallbacks
{
    /// <summary>Called after each panel is normalized and OCR'd (raw crop, normalized image, text).</summary>
    public Action<byte[], byte[], string>? OnPanelOcr { get; init; }
}
