using System.Text.Json;

namespace ObsidianRagEngine.Tests.Setup;

/// <summary>
/// Static store for the <c>___testdata/ocr</c> hierarchy: load cases, write per-model artifacts, consolidate scores.
/// </summary>
public static class OcrTestStore
{
    private const int MaxConsolidatedSnapshots = 10;

    private static readonly HashSet<string> ImageExtensions =
        new(StringComparer.OrdinalIgnoreCase) { ".png", ".jpg", ".jpeg", ".gif", ".webp", ".bmp" };

    private static readonly JsonSerializerOptions IndentedJson = new() { WriteIndented = true };

    public static IReadOnlyList<OcrTestCase> AllTestCases => AllTestCasesLazy.Value;

    private static readonly Lazy<IReadOnlyList<OcrTestCase>> AllTestCasesLazy = new(() =>
    {
        // Project-source ___testdata (gitignored, not copied to bin, available in local env)
        var rootDir = GetSourceTestdataRoot();
        if (!Directory.Exists(rootDir))
            return [];

        var testCases = EnumerateCaseDirectories(rootDir)
            .Select(LoadCase)
            .ToList();

        if (testCases.Count == 0)
        {
            throw new InvalidOperationException(
                "No OCR test cases found. Add folders under ___testdata/ocr/<case>/ with an image and expected.txt.");
        }

        return testCases;
    });

    public static void ResetResultFolder(OcrTestCase testCase, string ocrModel)
    {
        var folder = GetModelResultsFolder(testCase, ocrModel);
        if (Directory.Exists(folder))
            Directory.Delete(folder, recursive: true);
        Directory.CreateDirectory(folder);
    }

    public static void SaveResult(OcrTestCase testCase, string ocrModel, string actualText, double score)
    {
        var folder = GetModelResultsFolder(testCase, ocrModel);
        File.WriteAllText(Path.Combine(folder, "actual.txt"), actualText);
        File.WriteAllText(
            Path.Combine(folder, "results.json"),
            JsonSerializer.Serialize(new { score }, IndentedJson));
    }

    public static void SavePanelResult(
        OcrTestCase testCase,
        string ocrModel,
        byte[] rawPanel,
        byte[] normalizedPanel,
        string text)
    {
        var folder = GetModelResultsFolder(testCase, ocrModel);
        var panelDir = Path.Combine(folder, $"{Directory.GetDirectories(folder).Length:D2}");
        Directory.CreateDirectory(panelDir);

        File.WriteAllBytes(Path.Combine(panelDir, "raw.png"), rawPanel);
        File.WriteAllBytes(Path.Combine(panelDir, "normalized.png"), normalizedPanel);
        File.WriteAllText(Path.Combine(panelDir, "ocr.txt"), text);
    }

    /// <summary>
    /// Writes <c>results__yyyy-MM-dd_HH-mm.json</c> at the project OCR testdata root:
    /// <c>{ "case": { "model": scoreOrNull, ... }, ... }</c> (scores rounded to 3 decimals).
    /// </summary>
    public static void ConsolidateResults()
    {
        var ocrRoot = GetSourceTestdataRoot();
        if (!Directory.Exists(ocrRoot))
            return;

        var caseDirs = EnumerateCaseDirectories(ocrRoot).ToList();
        if (caseDirs.Count == 0)
            return;

        var modelNames = caseDirs
            .SelectMany(Directory.EnumerateDirectories)
            .Select(Path.GetFileName)
            .Where(name => !string.IsNullOrEmpty(name))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var consolidated = caseDirs.ToDictionary(
            caseDir => Path.GetFileName(caseDir)!,
            caseDir => modelNames.ToDictionary(
                modelName => modelName!,
                modelName => TryReadScore(Path.Combine(caseDir, modelName!, "results.json")),
                StringComparer.OrdinalIgnoreCase),
            StringComparer.OrdinalIgnoreCase);

        var outputPath = Path.Combine(ocrRoot, $"results__{DateTime.Now:yyyy-MM-dd_HH-mm}.json");
        File.WriteAllText(outputPath, JsonSerializer.Serialize(consolidated, IndentedJson));

        foreach (var path in Directory.EnumerateFiles(ocrRoot, "results__*.json")
                     .OrderByDescending(File.GetLastWriteTimeUtc)
                     .Skip(MaxConsolidatedSnapshots))
        {
            File.Delete(path);
        }
    }

    private static OcrTestCase LoadCase(string caseDir)
    {
        var caseName = Path.GetFileName(caseDir);
        var expectedPath = Path.Combine(caseDir, "expected.txt");
        if (!File.Exists(expectedPath))
            throw new InvalidOperationException($"OCR test case '{caseName}' is missing expected.txt.");

        var imagePath = Directory.EnumerateFiles(caseDir)
            .SingleOrDefault(path => ImageExtensions.Contains(Path.GetExtension(path)));

        if (imagePath is null)
        {
            throw new InvalidOperationException(
                $"OCR test case '{caseName}' must contain exactly one image file.");
        }

        return new OcrTestCase(imagePath, File.ReadAllText(expectedPath));
    }

    private static IEnumerable<string> EnumerateCaseDirectories(string ocrRoot) =>
        Directory.EnumerateDirectories(ocrRoot)
            .Where(dir => File.Exists(Path.Combine(dir, "expected.txt")))
            .OrderBy(dir => Path.GetFileName(dir), StringComparer.OrdinalIgnoreCase);

    private static double? TryReadScore(string resultsPath)
    {
        if (!File.Exists(resultsPath))
            return null;

        try
        {
            using var doc = JsonDocument.Parse(File.ReadAllText(resultsPath));
            if (doc.RootElement.TryGetProperty("score", out var score) &&
                score.ValueKind == JsonValueKind.Number &&
                score.TryGetDouble(out var value))
            {
                return Math.Round(value, 3);
            }
        }
        catch (JsonException)
        {
            // Treat unreadable results the same as missing.
        }

        return null;
    }

    private static string GetModelResultsFolder(OcrTestCase testCase, string ocrModel) =>
        Path.Combine(GetSourceTestdataRoot(), testCase.CaseName, SanitizeModelName(ocrModel));

    /// <summary>Project-source <c>___testdata/ocr</c> (artifacts written here, not under bin).</summary>
    private static string GetSourceTestdataRoot() =>
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "___testdata", "ocr"));

    private static string SanitizeModelName(string ocrModel) =>
        string.Join("_", ocrModel.Split(Path.GetInvalidFileNameChars(), StringSplitOptions.RemoveEmptyEntries));
}

/// <summary>
/// Image under test for OCR: path to the source file and the text expected after recognition.
/// </summary>
public sealed record OcrTestCase(string ImagePath, string ExpectedText)
{
    public string CaseName => Path.GetFileName(Path.GetDirectoryName(ImagePath)!);
}
