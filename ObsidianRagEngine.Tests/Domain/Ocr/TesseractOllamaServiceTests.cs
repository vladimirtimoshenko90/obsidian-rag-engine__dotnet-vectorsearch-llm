using FluentAssertions;
using ObsidianRagEngine.Ocr.Tesseract;
using ObsidianRagEngine.Tests.Setup;

namespace ObsidianRagEngine.Tests.Domain.Ocr;

public class TesseractOllamaServiceTests(TesseractOllamaFixture fixture) : IClassFixture<TesseractOllamaFixture>
{
    private const double MinimumSimilarity = 0.6;

    public static IEnumerable<object[]> OcrTestCases =>
        new TestSettingsFixture().OcrTestCases
            .Select(testCase => new object[] { testCase });

    [Theory]
    [MemberData(nameof(OcrTestCases))]
    public async Task ExtractText_FromSampleImage_MatchesExpectedText(OcrTestCase testCase)
    {
        // Arrange
        var imageBytes = await File.ReadAllBytesAsync(testCase.ImagePath);

        // Act
        var ocredText = await fixture.Sut.ExtractText(
            imageBytes,
            [TesseractLanguages.Russian, TesseractLanguages.English],
            CancellationToken.None);

        var score = TextComparer.Compare(ocredText, testCase.ExpectedText);

        fixture.Settings.SaveOcrResult(testCase, ocredText, score);

        // Assert
        score.Should().BeGreaterThanOrEqualTo(MinimumSimilarity);
    }
}
