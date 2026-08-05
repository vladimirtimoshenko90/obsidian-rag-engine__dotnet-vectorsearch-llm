using ObsidianRagEngine.Contracts;

namespace ObsidianRagEngine.Ocr.Domains.Messenger.Hinted;

/// <summary>
/// Thin wrapper: runs inner vision OCR on a full messenger composite image with a fixed
/// messenger clarification, optionally extended by a caller <c>clarificationPrompt</c>.
/// No split/merge — only prompt composition + delegate.
/// </summary>
public sealed class MessengerHintedOcrService(IOcrProvider inner) : IOcrProvider
{
    public string ModelName => $"messenger_hinted__{inner.ModelName}";

    public Task<LlmCallResult> ExtractText(
        byte[] imageBytes,
        IReadOnlyList<OcrLanguage> languages,
        CancellationToken ct,
        string? clarificationPrompt = null)
    {
        var prompt = MessengerHintedClarificationPrompts.Text;

        if (!string.IsNullOrWhiteSpace(clarificationPrompt))
            prompt += "\n\n" + MessengerHintedClarificationPrompts.AdditionalClarification(clarificationPrompt);

        return inner.ExtractText(imageBytes, languages, ct, prompt);
    }
}
