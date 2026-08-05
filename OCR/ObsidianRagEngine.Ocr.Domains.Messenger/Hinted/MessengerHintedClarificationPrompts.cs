namespace ObsidianRagEngine.Ocr.Domains.Messenger.Hinted;

/// <summary>
/// Clarification messages used by <see cref="MessengerHintedOcrService"/> for messenger
/// composite screenshots. Passed to the inner <see cref="Contracts.IOcrProvider"/> as a separate chat message.
/// </summary>
internal static class MessengerHintedClarificationPrompts
{
    public const string Text =
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
        """;

    /// <summary>
    /// Formats caller-supplied clarification as a titled block to append after <see cref="Text"/>.
    /// </summary>
    public static string AdditionalClarification(string clarificationPrompt) =>
        """
        Additional clarification:
        """ + clarificationPrompt.Trim();
}
