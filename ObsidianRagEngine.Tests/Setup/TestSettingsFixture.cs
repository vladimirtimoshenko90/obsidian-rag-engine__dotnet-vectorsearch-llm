using Microsoft.Extensions.Configuration;
using ObsidianRagEngine.Console.Common.Extensions;

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

    public TestSettingsFixture()
    {
        Configuration = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false)
            .AddEnvironmentVariables()
            .Build();

        TesseractUrl = Require("Tesseract:Url");
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

    private string Require(string key) =>
        Configuration[key].Valuable()
            ? Configuration[key]!
            : throw new InvalidOperationException($"Required setting '{key}' is missing or empty.");
}

/// <summary>
/// Image under test for OCR: path to the source file and the text expected after recognition.
/// </summary>
public sealed record OcrTestCase(string ImagePath, string ExpectedText);
