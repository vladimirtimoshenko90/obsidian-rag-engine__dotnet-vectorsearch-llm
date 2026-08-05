namespace ObsidianRagEngine.Contracts;

/// <summary>
/// One prompt's text in multiple languages.
/// Resolution: first matching entry in <paramref name="languages"/>, else English, else any entry.
/// </summary>
public sealed class LocalizedText
{
    private readonly Dictionary<OcrLanguage, string> _versions = new();

    public string this[OcrLanguage language]
    {
        set => _versions[language] = value;
    }

    public string Get(IReadOnlyList<OcrLanguage>? languages = null)
    {
        if (_versions.Count == 0)
            throw new InvalidOperationException("No localized text versions provided.");

        if (languages is { Count: > 0 })
        {
            foreach (var language in languages)
            {
                if (_versions.TryGetValue(language, out var text))
                    return text;
            }
        }

        if (_versions.TryGetValue(OcrLanguage.English, out var english))
            return english;

        return _versions.Values.First();
    }
}

/// <summary>
/// Named collection of prompts, each with its localized variants.
/// </summary>
public sealed class LocalizedTextSet
{
    private readonly Dictionary<string, LocalizedText> _texts = new();

    public LocalizedText this[string name]
    {
        get => _texts[name];
        set => _texts[name] = value;
    }

    public string Get(string name, IReadOnlyList<OcrLanguage>? languages = null) =>
        _texts[name].Get(languages);
}
