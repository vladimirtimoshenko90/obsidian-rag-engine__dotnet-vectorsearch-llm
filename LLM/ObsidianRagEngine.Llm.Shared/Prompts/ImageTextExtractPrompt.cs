using ObsidianRagEngine.Contracts;

namespace ObsidianRagEngine.Llm.Prompts;

internal static class ImageTextExtractPrompt
{
    static readonly LocalizedTextSet _prompts = new()
    {
        ["ExtractText"] = new LocalizedText
        {
            [OcrLanguage.English] =
                """
                Extract all readable text from the image in reading order (top to bottom, left to right).
                Preserve line breaks that separate messages or paragraphs.
                Copy text exactly: keep original language, spelling, punctuation, and casing.
                Do not describe the image, translate, summarize, or invent missing words.
                If there is no text, reply with an empty response.
                """,
            [OcrLanguage.Russian] =
                """
                Извлеки весь читаемый текст с изображения в порядке чтения (сверху вниз, слева направо).
                Сохраняй переносы строк, которые разделяют сообщения или абзацы.
                Копируй текст точно: сохраняй исходный язык, орфографию, пунктуацию и регистр.
                Не описывай изображение, не переводи, не суммаризируй и не выдумывай недостающие слова.
                Если текста нет, ответь пустым ответом.
                """,
        },
        ["LanguagesHint"] = new LocalizedText
        {
            [OcrLanguage.English] =
                """

                The text is expected to be primarily in: {languages}.
                Prefer those scripts/languages when disambiguating characters.
                """,
            [OcrLanguage.Russian] =
                """

                Текст ожидается преимущественно на: {languages}.
                При неоднозначности символов предпочитай эти языки/письменности.
                """,
        },
    };

    public static string Build(IReadOnlyList<OcrLanguage>? languages)
    {
        var prompt = _prompts.Get("ExtractText", languages);

        if (languages is { Count: > 0 })
        {
            prompt += _prompts
                .Get("LanguagesHint", languages)
                .Replace("{languages}", string.Join(", ", languages));
        }

        return prompt;
    }
}
