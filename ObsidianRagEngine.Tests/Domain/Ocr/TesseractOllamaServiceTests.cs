using FluentAssertions;
using ObsidianRagEngine.Console.Domain.Ocr;
using ObsidianRagEngine.Tests.Setup;

namespace ObsidianRagEngine.Tests.Domain.Ocr;

public class TesseractOllamaServiceTests(TesseractOllamaFixture fixture) : IClassFixture<TesseractOllamaFixture>
{
    private const double MinimumSimilarity = 0.6;

    public static IEnumerable<object[]> OcrTestCases =>
        new TestSettingsFixture().OcrTestCases
            .Select(testCase => new object[] { testCase.ImagePath, testCase.ExpectedText });

    [Theory]
    [MemberData(nameof(OcrTestCases))]
    public async Task ExtractText_FromSampleImage_MatchesExpectedText(string imagePath, string expectedText)
    {
        // Arrange
        var imageBytes = await File.ReadAllBytesAsync(imagePath);

        // Act
        var ocredText = await fixture.Sut.ExtractText(
            imageBytes,
            [TesseractLanguages.Russian, TesseractLanguages.English]);

        // Assert
        var score = TextComparer.Compare(ocredText, expectedText);
        score.Should().BeGreaterThanOrEqualTo(MinimumSimilarity);
    }
}
