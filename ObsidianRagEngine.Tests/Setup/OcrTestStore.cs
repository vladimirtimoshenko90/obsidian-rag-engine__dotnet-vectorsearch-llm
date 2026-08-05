using System.Text.Json;
using ObsidianRagEngine.Contracts;

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

    public static void SaveResult(OcrTestCase testCase, string ocrModel, LlmCallResult result, double score)
    {
        var folder = GetModelResultsFolder(testCase, ocrModel);
        File.WriteAllText(Path.Combine(folder, "actual.txt"), result.Text);
        File.WriteAllText(
            Path.Combine(folder, "results.json"),
            JsonSerializer.Serialize(
                new
                {
                    score,
                    cost = result.Cost,
                    inputTokens = result.Usage.InputTokens,
                    outputTokens = result.Usage.OutputTokens,
                },
                IndentedJson));
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
    /// <c>{ "byCase": { "case": { "model": score } }, "byModel": { "model": { "avgScore", "totalCost", "totalInputTokens", "totalOutputTokens", "successfulRuns", "successRate" } } }</c>
    /// <c>byCase</c> sorted by score desc; <c>byModel</c> sorted by avgScore desc.
    /// <c>successfulRuns</c> = folders with a readable <c>results.json</c>; <c>successRate</c> = integer percent of model folders that succeeded (e.g. <c>"80%"</c>).
    /// Failed runs (no <c>results.json</c>) are omitted from <c>byCase</c>, but still count in <c>byModel</c>
    /// toward <c>avgScore</c>, <c>totalCost</c>, and token totals as score/cost/tokens <c>0</c>.
    /// </summary>
    public static void ConsolidateResults()
    {
        var ocrRoot = GetSourceTestdataRoot();
        if (!Directory.Exists(ocrRoot))
            return;

        var caseDirs = EnumerateCaseDirectories(ocrRoot).ToList();
        if (caseDirs.Count == 0)
            return;

        var byCase = new Dictionary<string, Dictionary<string, double>>(StringComparer.OrdinalIgnoreCase);
        var accumulators = new Dictionary<string, ModelAccumulator>(StringComparer.OrdinalIgnoreCase);

        foreach (var caseDir in caseDirs)
        {
            var caseName = Path.GetFileName(caseDir)!;
            var caseScores = new List<(string Model, double Score)>();

            foreach (var modelDir in Directory.EnumerateDirectories(caseDir)
                         .OrderBy(Path.GetFileName, StringComparer.OrdinalIgnoreCase))
            {
                var modelName = Path.GetFileName(modelDir)!;
                if (!accumulators.TryGetValue(modelName, out var acc))
                    accumulators[modelName] = acc = new ModelAccumulator();

                acc.AddRun();

                var metrics = TryReadResult(Path.Combine(modelDir, "results.json"));
                if (metrics is null)
                    continue;

                caseScores.Add((modelName, metrics.Score));
                acc.AddSuccess(metrics);
            }

            // Insert in score-desc order so System.Text.Json emits a compact object in that order.
            var orderedScores = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
            foreach (var entry in caseScores
                         .OrderByDescending(e => e.Score)
                         .ThenBy(e => e.Model, StringComparer.OrdinalIgnoreCase))
            {
                orderedScores[entry.Model] = entry.Score;
            }

            byCase[caseName] = orderedScores;
        }

        var byModel = accumulators
            .Select(kv => (Model: kv.Key, Summary: kv.Value.ToSummary()))
            .OrderByDescending(entry => entry.Summary.AvgScore ?? double.MinValue)
            .ThenBy(entry => entry.Model, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                entry => entry.Model,
                entry => entry.Summary,
                StringComparer.OrdinalIgnoreCase);

        var document = new { byCase, byModel };
        var outputPath = Path.Combine(ocrRoot, $"results__{DateTime.Now:yyyy-MM-dd_HH-mm}.json");
        File.WriteAllText(outputPath, JsonSerializer.Serialize(document, IndentedJson));

        foreach (var path in Directory.EnumerateFiles(ocrRoot, "results__*.json")
                     .OrderByDescending(File.GetLastWriteTimeUtc)
                     .Skip(MaxConsolidatedSnapshots))
        {
            File.Delete(path);
        }
    }

    private sealed class ModelAccumulator
    {
        private double _scoreSum;
        private decimal _costSum;
        private long _inputTokensSum;
        private long _outputTokensSum;
        private int _runs;
        private int _successfulRuns;

        public void AddRun() => _runs++;

        public void AddSuccess(OcrRunMetrics metrics)
        {
            _scoreSum += metrics.Score;
            _costSum += metrics.Cost ?? 0m;
            _inputTokensSum += metrics.InputTokens;
            _outputTokensSum += metrics.OutputTokens;
            _successfulRuns++;
        }

        public OcrModelSummary ToSummary()
        {
            var successRatePercent = _runs > 0
                ? (int)Math.Round(100.0 * _successfulRuns / _runs)
                : 0;

            return new(
                AvgScore: _runs > 0 ? Math.Round(_scoreSum / _runs, 3) : null,
                TotalCost: Math.Round(_costSum, 4),
                TotalInputTokens: _inputTokensSum,
                TotalOutputTokens: _outputTokensSum,
                SuccessfulRuns: _successfulRuns,
                SuccessRate: $"{successRatePercent}%");
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

    private static OcrRunMetrics? TryReadResult(string resultsPath)
    {
        if (!File.Exists(resultsPath))
            return null;

        try
        {
            using var doc = JsonDocument.Parse(File.ReadAllText(resultsPath));
            if (!doc.RootElement.TryGetProperty("score", out var score) ||
                score.ValueKind != JsonValueKind.Number ||
                !score.TryGetDouble(out var scoreValue))
            {
                return null;
            }

            decimal? cost = null;
            if (doc.RootElement.TryGetProperty("cost", out var costEl) &&
                costEl.ValueKind == JsonValueKind.Number &&
                costEl.TryGetDecimal(out var costValue))
            {
                cost = costValue;
            }

            var inputTokens = TryReadInt(doc.RootElement, "inputTokens");
            var outputTokens = TryReadInt(doc.RootElement, "outputTokens");

            return new OcrRunMetrics(Math.Round(scoreValue, 3), cost, inputTokens, outputTokens);
        }
        catch (JsonException)
        {
            // Treat unreadable results the same as missing.
        }

        return null;
    }

    private static int TryReadInt(JsonElement root, string propertyName)
    {
        if (root.TryGetProperty(propertyName, out var el) &&
            el.ValueKind == JsonValueKind.Number &&
            el.TryGetInt32(out var value))
        {
            return value;
        }

        return 0;
    }

    private static string GetModelResultsFolder(OcrTestCase testCase, string ocrModel) =>
        Path.Combine(GetSourceTestdataRoot(), testCase.CaseName, SanitizeModelName(ocrModel));

    /// <summary>Project-source <c>___testdata/ocr</c> (artifacts written here, not under bin).</summary>
    private static string GetSourceTestdataRoot() =>
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "___testdata", "ocr"));

    private static string SanitizeModelName(string ocrModel) =>
        string.Join("_", ocrModel.Split(Path.GetInvalidFileNameChars(), StringSplitOptions.RemoveEmptyEntries));
}

/// <summary>Per-model metrics written into consolidated OCR result snapshots.</summary>
public sealed record OcrRunMetrics(double Score, decimal? Cost, int InputTokens, int OutputTokens);

/// <summary>Per-model rollup across cases in a consolidated snapshot.</summary>
public sealed record OcrModelSummary(
    double? AvgScore,
    decimal TotalCost,
    long TotalInputTokens,
    long TotalOutputTokens,
    int SuccessfulRuns,
    string SuccessRate);

/// <summary>
/// Image under test for OCR: path to the source file and the text expected after recognition.
/// </summary>
public sealed record OcrTestCase(string ImagePath, string ExpectedText)
{
    public string CaseName => Path.GetFileName(Path.GetDirectoryName(ImagePath)!);
}
