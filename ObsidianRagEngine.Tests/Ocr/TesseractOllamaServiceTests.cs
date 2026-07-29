using FluentAssertions;
using ObsidianRagEngine.Contracts;
using ObsidianRagEngine.Tests.Ocr.Helpers;
using ObsidianRagEngine.Tests.Setup;

namespace ObsidianRagEngine.Tests.Ocr;

public class TesseractOllamaServiceTests(OcrFixture fixture) : IClassFixture<OcrFixture>
{
    private const double MinimumSimilarity = 0.2;   // minimal accuracy is enough, tests are just checking that "something is detected" and ocr does not fail

    public static IEnumerable<object[]> OcrTestCases =>
        new TestSettingsFixture().OcrTestCases
            .Select(testCase => new object[] { testCase });

    [Theory]
    [MemberData(nameof(OcrTestCases))]
    public async Task ExtractText_FromSampleImage_MatchesExpectedText(OcrTestCase testCase)
    {
        // Arrange
        var imageBytes = await File.ReadAllBytesAsync(testCase.ImagePath);

        var sut = fixture.Tesseract;
        fixture.Settings.ResetOcrResultFolder(testCase, sut.ModelName);

        // Act
        var ocredText = await sut.ExtractText(
            imageBytes,
            [OcrLanguage.Russian, OcrLanguage.English],
            CancellationToken.None);

        var score = TextComparer.Compare(ocredText, testCase.ExpectedText);

        fixture.Settings.SaveOcrResult(testCase, sut.ModelName, ocredText, score);

        // Assert
        score.Should().BeGreaterThanOrEqualTo(MinimumSimilarity);
    }
}
