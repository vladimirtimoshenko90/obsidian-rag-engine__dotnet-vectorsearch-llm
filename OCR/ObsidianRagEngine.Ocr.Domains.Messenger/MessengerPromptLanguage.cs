using ObsidianRagEngine.Contracts;

namespace ObsidianRagEngine.Ocr.Domains.Messenger;

/// <summary>
/// Picks the prompt language for messenger OCR: first input language that has localized
/// prompts, otherwise English.
/// </summary>
internal static class MessengerPromptLanguage
{
    private static readonly HashSet<OcrLanguage> Supported =
    [
        OcrLanguage.English,
        OcrLanguage.Russian,
    ];

    public static OcrLanguage Resolve(IReadOnlyList<OcrLanguage>? languages)
    {
        if (languages is { Count: > 0 })
        {
            foreach (var language in languages)
            {
                if (Supported.Contains(language))
                    return language;
            }
        }

        return OcrLanguage.English;
    }
}
