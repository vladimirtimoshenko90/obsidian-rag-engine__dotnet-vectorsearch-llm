using Microsoft.Extensions.Configuration;
using ObsidianRagEngine.Console.Common.Extensions;

namespace ObsidianRagEngine.Tests.Setup;

/// <summary>
/// xUnit class fixture: constructed once per test class and injected via constructor.
/// </summary>
public sealed class TestSettingsFixture
{
    public IConfiguration Configuration { get; }

    public IReadOnlyList<OcrTestCase> OcrTestCases { get; }

    public string TesseractUrl { get; }

    public TestSettingsFixture()
    {
        Configuration = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false)
            .AddJsonFile("appsettings.local.json", optional: false)
            .AddEnvironmentVariables()
            .Build();

        TesseractUrl = Require("Tesseract:Url");
        OcrTestCases = LoadOcrTestCases();

        if (OcrTestCases.Count == 0)
            throw new InvalidOperationException("OcrTestCases must contain at least one entry in appsettings.local.json.");
    }

    private IReadOnlyList<OcrTestCase> LoadOcrTestCases()
    {
        var testCases = new List<OcrTestCase>();

        foreach (var child in Configuration.GetSection("OcrTestCases").GetChildren())
        {
            var imagePath = child["ImagePath"];
            var expectedText = child["ExpectedText"];

            if (!imagePath.Valuable() || !expectedText.Valuable())
            {
                throw new InvalidOperationException(
                    "Each OcrTestCases entry requires non-empty ImagePath and ExpectedText.");
            }

            testCases.Add(new OcrTestCase(imagePath!, expectedText!));
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
