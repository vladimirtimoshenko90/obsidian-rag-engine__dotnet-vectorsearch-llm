using ObsidianRagEngine.Contracts;

namespace ObsidianRagEngine.Llm.Prompts;

internal static class ImageTextExtractPrompt
{
    private const string BaseText =
        """
        Extract all readable text from the image in reading order (top to bottom, left to right).
        Preserve line breaks that separate messages or paragraphs.
        Copy text exactly: keep original language, spelling, punctuation, and casing.
        Do not describe the image, translate, summarize, or invent missing words.
        If there is no text, reply with an empty response.
        """;

    public static string Build(IReadOnlyList<OcrLanguage>? languages)
    {
        var prompt = BaseText;

        if (languages is { Count: > 0 })
        {
            prompt +=
                $"\nThe text is expected to be primarily in: {string.Join(", ", languages)}."
                + " Prefer those scripts/languages when disambiguating characters.";
        }

        return prompt;
    }
}
