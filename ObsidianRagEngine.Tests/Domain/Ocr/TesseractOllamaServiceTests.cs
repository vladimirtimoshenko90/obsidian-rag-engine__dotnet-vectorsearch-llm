using FluentAssertions;
using ObsidianRagEngine.Console.Domain.Ocr;
using ObsidianRagEngine.Tests.Setup;

namespace ObsidianRagEngine.Tests.Domain.Ocr;

public class TesseractOllamaServiceTests(TesseractOllamaFixture fixture) : IClassFixture<TesseractOllamaFixture>
{
    private const double MinimumSimilarity = 0.6;

    [Fact]
    public async Task ExtractText_FromSampleChatScreenshot_MatchesExpectedText()
    {
        // Arrange
        var imageBytes = await File.ReadAllBytesAsync(fixture.Settings.OcrSampleImagePath);

        // Act
        var ocredText = await fixture.Sut.ExtractText(
            imageBytes,
            [TesseractLanguages.Russian, TesseractLanguages.English]);

        // Assert
        var score = TextComparer.Compare(ocredText, fixture.Settings.OcrExpectedText);
        score.Should().BeGreaterThanOrEqualTo(MinimumSimilarity);
    }
}
