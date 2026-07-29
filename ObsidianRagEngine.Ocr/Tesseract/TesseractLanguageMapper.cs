using ObsidianRagEngine.Contracts;

namespace ObsidianRagEngine.Ocr.Tesseract;

/// <summary>
/// Maps shared <see cref="OcrLanguage"/> values to Alpine tesseract-ocr-data pack IDs.
/// </summary>
public static class TesseractLanguageMapper
{
    public static IReadOnlyList<string> ToTesseractCodes(IReadOnlyList<OcrLanguage> languages)
    {
        ArgumentNullException.ThrowIfNull(languages);

        var result = new string[languages.Count];
        for (var i = 0; i < languages.Count; i++)
            result[i] = ToTesseractCode(languages[i]);
        return result;
    }

    public static string ToTesseractCode(OcrLanguage language) => language switch
    {
        OcrLanguage.English => TesseractLanguages.English,
        OcrLanguage.Russian => TesseractLanguages.Russian,
        _ => throw new ArgumentOutOfRangeException(nameof(language), language, "Unsupported OCR language.")
    };
}
