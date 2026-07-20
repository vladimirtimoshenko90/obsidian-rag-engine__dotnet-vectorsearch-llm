using Microsoft.Extensions.Configuration;
using ObsidianRagEngine.Console.Common.Extensions;

namespace ObsidianRagEngine.Tests.Setup;

/// <summary>
/// xUnit class fixture: constructed once per test class and injected via constructor.
/// </summary>
public sealed class TestSettingsFixture
{
    public IConfiguration Configuration { get; }

    public string OcrSampleImagePath { get; }
    public string OcrExpectedText { get; }

    public string TesseractUrl { get; }

    public TestSettingsFixture()
    {
        Configuration = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false)
            .AddJsonFile("appsettings.local.json", optional: false)
            .AddEnvironmentVariables()
            .Build();

        OcrSampleImagePath = Require("Ocr:SampleImagePath");
        OcrExpectedText = Require("Ocr:ExpectedText");
        TesseractUrl = Require("Tesseract:Url");
    }

    private string Require(string key) =>
        Configuration[key].Valuable()
            ? Configuration[key]!
            : throw new InvalidOperationException($"Required setting '{key}' is missing or empty.");
}
