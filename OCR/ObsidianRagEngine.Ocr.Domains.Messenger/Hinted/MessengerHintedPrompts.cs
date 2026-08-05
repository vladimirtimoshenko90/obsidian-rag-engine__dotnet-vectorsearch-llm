using ObsidianRagEngine.Contracts;

namespace ObsidianRagEngine.Ocr.Domains.Messenger.Hinted;

/// <summary>
/// Clarification messages for messenger composite screenshots.
/// Passed to the inner <see cref="IOcrProvider"/> as a separate chat message.
/// </summary>
internal static class MessengerHintedPrompts
{
    static readonly LocalizedTextSet _prompts = new()
    {
        [nameof(ExtractImageContents)] = new LocalizedText
        {
            [OcrLanguage.English] =
                """
                Image contents (read carefully before extracting text):

                The attached image is a composite of mobile messenger chat screenshots. One or more
                phone screens of the same conversation are placed side by side horizontally (left to
                right), as if consecutive scrolls or devices were concatenated into a single wide image.
                Vertical seams between panels may be visible; each column is one phone viewport.

                Reading order:
                1. Process panels from left to right.
                2. Within each panel, read from top to bottom in normal chat chronological order
                   (older messages higher, newer messages lower, unless the UI clearly shows otherwise).
                3. Continue across panels so the overall transcript follows the conversation timeline,
                   not a single left-to-right sweep across all columns at once.

                What to extract:
                - Message body text (bubbles), including slang, typos, emoji, and punctuation as shown.
                - Visible participant names / titles when they label messages.
                - Conversation date or time labels that belong to the chat history (e.g. day separators,
                  message timestamps), when they are part of the dialogue layout.

                What to omit:
                - Device and app chrome: status bar, signal/battery icons, navigation bars, tabs,
                  toolbars, input field / keyboard, send buttons.
                - Decorative UI only: reaction chips, unread badges, avatars as images (not name text),
                  stickers without readable text, blurred or non-textual media placeholders.

                Output rules:
                - Return only the conversation text in the reading order above.
                - Preserve original languages (often mixed), spelling, casing, and line breaks between messages.
                - Do not describe the screenshot, summarize, translate, or invent missing words.
                """,
            [OcrLanguage.Russian] =
                """
                Содержимое изображения (внимательно прочитай перед извлечением текста):

                Приложенное изображение — составной снимок экранов мобильного мессенджера. Один или
                несколько экранов одной и той же переписки расположены рядом по горизонтали (слева
                направо), как если бы последовательные прокрутки или устройства склеили в одну широкую
                картинку. Между панелями могут быть видны вертикальные швы; каждый столбец — один
                телефонный экран.

                Порядок чтения:
                1. Обрабатывай панели слева направо.
                2. Внутри каждой панели читай сверху вниз в обычном хронологическом порядке чата
                   (старые сообщения выше, новые ниже, если UI явно не показывает иное).
                3. Переходи между панелями так, чтобы итоговый текст следовал хронологии диалога,
                   а не одним проходом слева направо по всем столбцам сразу.

                Что извлекать:
                - Текст сообщений (пузыри), включая сленг, опечатки, эмодзи и пунктуацию как на экране.
                - Видимые имена / заголовки участников, если они подписывают сообщения.
                - Даты и время, относящиеся к истории чата (разделители дней, метки сообщений), если
                  они часть макета диалога.

                Что пропускать:
                - Системный и приложенийный хром: статус-бар, иконки связи/батареи, панели навигации,
                  вкладки, тулбары, поле ввода / клавиатуру, кнопку отправки.
                - Только декоративный UI: реакции, бейджи непрочитанного, аватары как картинки
                  (не текст имени), стикеры без читаемого текста, размытые или нетекстовые плейсхолдеры медиа.

                Правила вывода:
                - Верни только текст переписки в порядке чтения выше.
                - Сохрани исходные языки (часто смешанные), орфографию, регистр и переносы между сообщениями.
                - Не описывай скриншот, не суммаризируй, не переводи и не выдумывай недостающие слова.
                """,
        },
        [nameof(AdditionalClarification)] = new LocalizedText
        {
            [OcrLanguage.English] = "Additional clarification:\n{clarification}",
            [OcrLanguage.Russian] = "Дополнительное уточнение:\n{clarification}",
        },
    };

    public static string ExtractImageContents(IReadOnlyList<OcrLanguage>? languages) =>
        _prompts.Get(nameof(ExtractImageContents), languages);

    public static string AdditionalClarification(IReadOnlyList<OcrLanguage>? languages, string clarificationPrompt) =>
        _prompts
            .Get(nameof(AdditionalClarification), languages)
            .Replace("{clarification}", clarificationPrompt.Trim());
}
