using Microsoft.Extensions.Configuration;
using ObsidianRagEngine.Console.Common.Extensions;
using System.Text.Json;

namespace ObsidianRagEngine.Tests.Setup;

/// <summary>
/// xUnit class fixture: constructed once per test class and injected via constructor.
/// </summary>
public sealed class TestSettingsFixture
{
    private static readonly HashSet<string> ImageExtensions =
        new(StringComparer.OrdinalIgnoreCase) { ".png", ".jpg", ".jpeg", ".gif", ".webp", ".bmp" };

    public IConfiguration Configuration { get; }

    public IReadOnlyList<OcrTestCase> OcrTestCases { get; }

    public string TesseractUrl { get; }

    public string OllamaUrl { get; }
    public string OllamaLlmModel { get; }

    public TestSettingsFixture()
    {
        Configuration = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false)
            .AddEnvironmentVariables()
            .Build();

        TesseractUrl = Require("Tesseract:Url");
        OllamaUrl = Require("Ollama:Url");
        OllamaLlmModel = Require("Ollama:LlmModel");
        OcrTestCases = LoadOcrTestCases();

        if (OcrTestCases.Count == 0)
        {
            throw new InvalidOperationException(
                "No OCR test cases found. Add folders under ___testdata/ocr/<case>/ with an image and expected.txt.");
        }
    }

    private IReadOnlyList<OcrTestCase> LoadOcrTestCases()
    {
        var root = Path.Combine(AppContext.BaseDirectory, "___testdata", "ocr");
        if (!Directory.Exists(root))
            return [];

        var testCases = new List<OcrTestCase>();

        foreach (var caseDir in Directory.EnumerateDirectories(root))
        {
            var expectedPath = Path.Combine(caseDir, "expected.txt");
            if (!File.Exists(expectedPath))
            {
                throw new InvalidOperationException(
                    $"OCR test case '{Path.GetFileName(caseDir)}' is missing expected.txt.");
            }

            var imagePath = Directory.EnumerateFiles(caseDir)
                .SingleOrDefault(path => ImageExtensions.Contains(Path.GetExtension(path)));

            if (imagePath is null)
            {
                throw new InvalidOperationException(
                    $"OCR test case '{Path.GetFileName(caseDir)}' must contain exactly one image file.");
            }

            testCases.Add(new OcrTestCase(imagePath, File.ReadAllText(expectedPath)));
        }

        return testCases;
    }

    public void ResetOcrResultFolder(OcrTestCase testCase, string ocrModel)
    {
        var modelResultsFolder = GetModelResultsFolder(testCase, ocrModel);
        if (Directory.Exists(modelResultsFolder))
            Directory.Delete(modelResultsFolder, recursive: true);
        Directory.CreateDirectory(modelResultsFolder);
    }

    public void SaveOcrResult(OcrTestCase testCase, string ocrModel, string actualText, double score)
    {
        var modelResultsFolder = GetModelResultsFolder(testCase, ocrModel);

        File.WriteAllText(
            Path.Combine(modelResultsFolder, "actual.txt"), 
            actualText);
        File.WriteAllText(
            Path.Combine(modelResultsFolder, "results.json"),
            JsonSerializer.Serialize(new { score }, new JsonSerializerOptions { WriteIndented = true }));
    }

    public void SavePanelOcrResult(
        OcrTestCase testCase,
        string ocrModel,
        byte[] rawPanel,
        byte[] normalizedPanel,
        string text)
    {
        var modelResultsFolder = GetModelResultsFolder(testCase, ocrModel);

        var panelDir = Path.Combine(modelResultsFolder, $"{Directory.GetDirectories(modelResultsFolder).Length:D2}");
        Directory.CreateDirectory(panelDir);

        File.WriteAllBytes(Path.Combine(panelDir, "raw.png"), rawPanel);
        File.WriteAllBytes(Path.Combine(panelDir, "normalized.png"), normalizedPanel);
        File.WriteAllText(Path.Combine(panelDir, "ocr.txt"), text);
    }

    /// <summary>
    /// Discovers every case/model results folder under <c>___testdata/ocr</c> and writes a
    /// consolidated <c>results__yyyy-MM-dd_HH-mm.json</c> at the OCR root (scores rounded to 3 decimals):
    /// <c>{ "case": { "model": scoreOrNull, ... }, ... }</c>.
    /// Missing per-model <c>results.json</c> files become <c>null</c>.
    /// </summary>
    public static void ConsolidateOcrResults()
    {
        var ocrRoot = GetOcrTestdataRoot();
        if (!Directory.Exists(ocrRoot))
            return;

        var caseDirs = Directory.EnumerateDirectories(ocrRoot)
            .Where(dir => File.Exists(Path.Combine(dir, "expected.txt")))
            .OrderBy(dir => Path.GetFileName(dir), StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (caseDirs.Count == 0)
            return;

        // Union of model subfolder names found under any case (nothing hardcoded).
        var modelNames = caseDirs
            .SelectMany(Directory.EnumerateDirectories)
            .Select(Path.GetFileName)
            .Where(name => !string.IsNullOrEmpty(name))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var consolidated = new Dictionary<string, Dictionary<string, double?>>(StringComparer.OrdinalIgnoreCase);

        foreach (var caseDir in caseDirs)
        {
            var caseName = Path.GetFileName(caseDir)!;
            var perModel = new Dictionary<string, double?>(StringComparer.OrdinalIgnoreCase);

            foreach (var modelName in modelNames)
            {
                var resultsPath = Path.Combine(caseDir, modelName!, "results.json");
                perModel[modelName!] = TryReadScore(resultsPath);
            }

            consolidated[caseName] = perModel;
        }

        var stamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm");
        var outputPath = Path.Combine(ocrRoot, $"results__{stamp}.json");
        if (File.Exists(outputPath))
            File.Delete(outputPath);

        File.WriteAllText(
            outputPath,
            JsonSerializer.Serialize(consolidated, new JsonSerializerOptions { WriteIndented = true }));

        // Keep only the newest consolidated snapshots.
        const int maxConsolidatedResults = 10;
        var outdated = Directory.EnumerateFiles(ocrRoot, "results__*.json")
            .OrderByDescending(File.GetLastWriteTimeUtc)
            .Skip(maxConsolidatedResults)
            .ToList();

        foreach (var path in outdated)
            File.Delete(path);
    }

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
        Path.Combine(GetOcrTestdataRoot(), testCase.CaseName, SanitizeModelName(ocrModel));

    private static string GetOcrTestdataRoot() =>
        Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory, "..", "..", "..",
            "___testdata", "ocr"));

    private static string SanitizeModelName(string ocrModel) =>
        string.Join("_", ocrModel.Split(Path.GetInvalidFileNameChars(), StringSplitOptions.RemoveEmptyEntries));

    private string Require(string key) =>
        Configuration[key].Valuable()
            ? Configuration[key]!
            : throw new InvalidOperationException($"Required setting '{key}' is missing or empty.");
}

/// <summary>
/// Image under test for OCR: path to the source file and the text expected after recognition.
/// </summary>
public sealed record OcrTestCase(string ImagePath, string ExpectedText)
{
    public string CaseName => Path.GetFileName(Path.GetDirectoryName(ImagePath)!);
}
