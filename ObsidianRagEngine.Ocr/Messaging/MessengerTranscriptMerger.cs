using System.Text;
using ObsidianRagEngine.Llm;

namespace ObsidianRagEngine.Ocr.Messaging;

/// <summary>
/// Merges ordered messenger-panel OCR strings into one cleaned chat transcript.
/// </summary>
public interface IMessengerTranscriptMerger
{
    Task<string> MergeAsync(IReadOnlyList<string> panelTexts, CancellationToken ct = default);
}

/// <summary>
/// Uses an <see cref="ILlmService"/> to merge and clean messenger-panel OCR text.
/// LLM backend is injectable (Ollama, cloud API, etc.).
/// </summary>
public sealed class MessengerTranscriptMerger(ILlmService llm) : IMessengerTranscriptMerger
{
    private readonly ILlmService _llm = llm ?? throw new ArgumentNullException(nameof(llm));

    private const string MergeInstructions =
        """
        You are given OCR text from one or more screenshot panels of the same chat,
        listed left to right. Merge them into one continuous transcript in reading order.

        Remove duplicated text caused by overlapping panels, including duplicated contact
        headers and status lines that repeat at the start of each panel.

        Remove messenger chrome and UI trash from the start of panels and elsewhere:
        status bars, battery, signal, app name, contact header, last-seen / online status
        lines (e.g. patterns like "В сети:"), match percentage, avatars, and icons.

        Keep only the chat messages themselves.
        Preserve only timestamps that clearly look like time (e.g. "14:32", "6:45")
        or date headers (e.g. "31 октября", "1 ноября", "Yesterday").
        Discard other numbers that appear to be UI artifacts (e.g. battery percentage,
        signal strength, match %).
        Separate different messages with a blank line between them.
        Do not correct any words. Only remove UI trash, duplicates, and formatting artifacts.
        Preserve all original text exactly as OCR provided it, including slang and typos.
        Use only text that appears in the provided OCR inputs — do not invent, translate,
        or add messages that are not present.

        Example:
        Panel 1:
        Alex
        last seen yesterday at 16:43
        Hey!
        14:32

        Panel 2:
        Alex
        last seen yesterday at 16:43
        How are you?
        14:35

        Output:
        Hey!
        14:32

        How are you?
        14:35

        Return only the cleaned transcript, with no commentary.
        """;

    public async Task<string> MergeAsync(IReadOnlyList<string> panelTexts, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(panelTexts);

        if (panelTexts.Count == 0)
            return string.Empty;

        var prompt = BuildPrompt(panelTexts);
        var result = await _llm.Generate(prompt, ct);
        return result.Trim();
    }

    private static string BuildPrompt(IReadOnlyList<string> panelTexts)
    {
        var sb = new StringBuilder();
        sb.AppendLine(MergeInstructions.Trim());
        sb.AppendLine();

        for (var i = 0; i < panelTexts.Count; i++)
        {
            sb.Append("Panel ").Append(i + 1).AppendLine(":");
            sb.AppendLine(panelTexts[i] ?? string.Empty);
            if (i < panelTexts.Count - 1)
                sb.AppendLine();
        }

        return sb.ToString();
    }
}
