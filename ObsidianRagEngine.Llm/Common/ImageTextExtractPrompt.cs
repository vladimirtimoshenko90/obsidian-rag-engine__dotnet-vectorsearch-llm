namespace ObsidianRagEngine.Llm.Common;

internal static class ImageTextExtractPrompt
{
    public const string Text =
        """
        Extract all readable text from the image in reading order (top to bottom, left to right).
        Preserve line breaks that separate messages or paragraphs.
        Copy text exactly: keep original language, spelling, punctuation, and casing.
        Do not describe the image, translate, summarize, or invent missing words.
        If there is no text, reply with an empty response.
        """;
}
