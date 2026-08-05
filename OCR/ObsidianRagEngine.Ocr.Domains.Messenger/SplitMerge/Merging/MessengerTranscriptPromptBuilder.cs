using System.Text;
using ObsidianRagEngine.Contracts;

namespace ObsidianRagEngine.Ocr.Domains.Messenger.SplitMerge.Merging;

/// <summary>
/// Builds the LLM prompt that merges ordered messenger-panel OCR strings into one cleaned chat transcript.
/// </summary>
public static class MessengerTranscriptPromptBuilder
{
    public static string BuildPrompt(IReadOnlyList<string> panelTexts, IReadOnlyList<OcrLanguage>? languages = null)
    {
        var promptLanguage = MessengerPromptLanguage.Resolve(languages);
        var (instructions, panelLabel) = promptLanguage switch
        {
            OcrLanguage.Russian => (MergeInstructionsRu, "Панель"),
            _ => (MergeInstructionsEn, "Panel"),
        };

        var sb = new StringBuilder();
        sb.AppendLine(instructions.Trim());
        sb.AppendLine();

        for (var i = 0; i < panelTexts.Count; i++)
        {
            sb.AppendLine($"------ {panelLabel} {i + 1}:");
            sb.AppendLine(panelTexts[i] ?? string.Empty);
            if (i < panelTexts.Count - 1)
                sb.AppendLine();
        }

        return sb.ToString();
    }

    private const string MergeInstructionsEn =
        """
        You receive several OCR fragments of one messenger conversation. The fragments are already ordered in time.

        Task: restore a single coherent dialogue as close as possible to the original, keeping all replies, dates, emotional markers, and chronology.

        ─── PROCEDURE ───

        STEP 1. Remove technical noise (EVERYWHERE, not only at the start)
        - Phone numbers (the same one across fragments, e.g. +375 33 634 4497) — delete completely.
        - Timestamps at the start of lines (formats like 17:35, 17:36, etc.) — delete.
        - Lone symbols that are not part of words: @, #, $, &, ¥, €, \, /, {, }, [, ], », «, ~, `, ', " — delete EVERYWHERE they appear, including inside lines.
        - Brackets and periods — delete ONLY when they are not smileys (e.g. ) , )) , ;) , :D — keep; lone brackets/periods standing apart from words — delete).
        - Any single letter standing alone that is not a real word (e.g. fragments; pronouns like "I"/"я" may stay only if clearly part of a reply) — delete.
        - Meaningless digit/letter scraps: "13 и", "4 пна", "Amn", "Mnoanane", "е а1ЕИ", "Py Паоеаллыекоя" — delete.
        - Technical labels like "Panel 1" / "Панель 1" — delete if they appear.

        STEP 2. Fix OCR errors
        - OCR often confuses Cyrillic and Latin lookalikes. Consider replacements such as:
          в→b, о→o, а→a, р→p, с→c, х→x, и→n, ш→w, щ→u, м→m, н→h, к→k, е→e, т→t (and the reverse when Latin was intended as Cyrillic).
        - Restore words from context, not mechanically.
        - Required corrections for this dialogue when they appear:
          "прекрасма" → "прекрасна"
          "минкт" → "минут"
          "посмусь"/"просмусь" → "проснусь"
          "предпожить" → "предложить"
          "Cynep" → "Супер"
          "Xanoy" → "Хелоу" (greeting)
          "Bosa"/"B0" → "Вова" (interlocutor name)
          "Алимв"/"Алима"/"Алена" → "Алина" (one person; unify spelling)
          "Люсика" → "Лосика" (street)
          "болего" → "белого"
          "бодяжим" → leave as-is (slang)
          "вжастик" → "ужастик"
          "интеесный" → "интересный"
        - If a word cannot be restored confidently — leave it as-is; do not invent.

        STEP 3. Join fragments (stitch and drop duplicates) — CRITICAL
        - Compare the END of each fragment with the START of the next.
        - If the same message (or a heavily distorted version) appears at the end of one fragment and the start of the next — output it ONLY ONCE.
        - Use the last ~150–200 characters of the previous fragment and the first ~150–200 of the next. If they match in meaning by ~60% or more (even with different OCR distortions) — remove that message from the start of the next fragment.
        - Do NOT remove repeats that are different replies from different speakers (e.g. one person asking again). If it is clearly the same utterance — keep only one copy.
        - If a fragment ends mid-word and the next starts with the same word — join into one message (e.g. "часа ма пол" + "пол раньше 8" → "часа на пол раньше 8").
        - Keep all unique replies — lose nothing except clear seam duplicates.

        STEP 4. Formatting and style
        - Each reply on a new line.
        - Blank line between meaningful blocks (day change, topic change).
        - If the source has dates (e.g. "12 июня") — keep them as separators before the matching block.
        - Keep smileys ()) , ;) , !) and interjections (Мммм, Оки, Ага) — they are part of natural speech.
        - Do not polish into literary language. Keep colloquial constructions, slang, intentional mistakes (if they are not OCR artifacts).

        STEP 5. Quality check before output (REQUIRED)
        - Ensure no unique reply was lost — compare cleaned source line counts with the result.
        - Ensure chronology is intact.
        - Check names: Алина and Вова should be consistent across the dialogue.
        - Carefully ensure no reply is duplicated twice (except when they are truly different replies). If near-identical phrases appear in different places — keep only the first.

        ─── OUTPUT ───
        Output only the final dialogue. No explanations, comments, technical labels, or change lists. Conversation text only.
        """;

    private const string MergeInstructionsRu =
        """
        Ты получаешь несколько OCR-фрагментов одной переписки в мессенджере. Фрагменты уже отсортированы по времени.

        Задача: восстановить единый связный диалог максимально близко к оригиналу, сохранив все реплики, даты, эмоциональные маркеры и хронологию.

        ─── АЛГОРИТМ ДЕЙСТВИЙ ───

        ШАГ 1. Удаление технического мусора (ВЕЗДЕ, не только в начале)
        - Номера телефонов (во всех фрагментах один и тот же: +375 33 634 4497) — удаляй полностью.
        - Временные метки в начале строк (формата 17:35, 17:36 и т.п.) — удаляй.
        - Все одиночные символы, не являющиеся частью слов: @, #, $, &, ¥, €, \, /, {, }, [, ], », «, ~, `, ', " — удаляй ВЕЗДЕ, где они встречаются, даже внутри строк.
        - Скобки и точки удаляй ТОЛЬКО если они не являются смайлами (например, ) , )) , ;) , :D — оставляй; одиночные скобки и точки, стоящие отдельно от слов — удаляй).
        - Любую одиночную букву (например, "з", "й", "о", "я" и т.п.), которая стоит отдельно и не является полноценным словом (местоимение "я" может остаться, но только если оно точно является частью реплики, а не обрывком), — удаляй.
        - Любые бессмысленные цифровые или буквенные обрывки: "13 и", "4 пна", "Amn", "Mnoanane", "е а1ЕИ", "Py Паоеаллыекоя" — удаляй.
        - Технические надписи вроде "Панель 1" — удаляй, если появятся.

        ШАГ 2. Исправление OCR-ошибок
        - OCR часто путает кириллицу и латиницу. Учитывай следующие замены:
          в→b, о→o, а→a, р→p, с→c, х→x, и→n, ш→w, щ→u, м→m, н→h, к→k, е→e, т→t.
        - Восстанавливай слова по контексту, а не механически.
        - Обязательные исправления для этого диалога:
          "прекрасма" → "прекрасна"
          "минкт" → "минут"
          "посмусь"/"просмусь" → "проснусь"
          "предпожить" → "предложить"
          "Cynep" → "Супер"
          "Xanoy" → "Хелоу" (приветствие)
          "Bosa"/"B0" → "Вова" (имя собеседника)
          "Алимв"/"Алима"/"Алена" → "Алина" (одно лицо, приведи к единому написанию)
          "Люсика" → "Лосика" (улица)
          "болего" → "белого"
          "бодяжим" → оставь как есть (сленг)
          "вжастик" → "ужастик"
          "интеесный" → "интересный"
        - Если слово не поддаётся уверенному восстановлению — оставь как есть, не додумывай.

        ШАГ 3. Объединение фрагментов (склейка и удаление дублей) — ЭТО КРИТИЧЕСКИ ВАЖНО
        - Ты должен сравнить КОНЕЦ каждого фрагмента с НАЧАЛОМ следующего.
        - Если одно и то же сообщение (или его сильно искажённая версия) встречается в конце одного фрагмента и в начале следующего — оно должно быть выведено ТОЛЬКО ОДИН РАЗ.
        - Для этого бери последние 150–200 символов предыдущего фрагмента и первые 150–200 символов следующего. Если они совпадают по смыслу на 60% и более (даже при разных OCR-искажениях) — удали это сообщение из начала следующего фрагмента.
        - НЕ удаляй повторы, если это разные реплики разных собеседников (например, один переспрашивает, другой повторяет). Но если это явно одно и то же высказывание — оставляй только один экземпляр.
        - Если фрагмент обрывается на полуслове, а следующий начинается с того же самого слова — склей их в одно сообщение (например, "часа ма пол" + "пол раньше 8" → "часа на пол раньше 8").
        - Все уникальные реплики должны быть сохранены — не теряй ничего, кроме явных дублей на стыках.

        ШАГ 4. Форматирование и стиль
        - Каждая реплика — с новой строки.
        - Между смысловыми блоками (смена дня, смена темы) — пустая строка.
        - Если в исходнике есть даты (например, "12 июня") — сохрани их как разделители и поставь перед соответствующим блоком.
        - Сохрани смайлы ()) , ;) , !) и междометия (Мммм, Оки, Ага) — они часть живой речи.
        - Не причесывай диалог до литературного языка. Оставь разговорные конструкции, сленг, намеренные ошибки (если они не OCR-артефакты).

        ШАГ 5. Контроль качества перед выводом (ОБЯЗАТЕЛЬНО)
        - Проверь, что ни одна уникальная реплика не потеряна — сравни общее количество строк в исходных фрагментах (очищенных от мусора) и в итоге.
        - Убедись, что хронология не нарушена.
        - Проверь имена: Алина и Вова должны быть единообразны во всём диалоге.
        - ОСОБО ВНИМАТЕЛЬНО проверь, что ни одна реплика не повторяется дважды (кроме случаев, когда это действительно разные реплики разных собеседников). Если видишь одинаковые или почти одинаковые фразы в разных местах итогового диалога — удали лишние, оставив только первый экземпляр.

        ─── ВЫВОД ───
        Выдай только итоговый диалог. Никаких пояснений, комментариев, технических меток, списка изменений. Только текст переписки.
        """;
}
