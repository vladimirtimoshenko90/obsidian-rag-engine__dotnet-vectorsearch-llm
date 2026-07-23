using FluentAssertions;
using ObsidianRagEngine.Ocr.Messaging;
using ObsidianRagEngine.Ocr.Tesseract;
using ObsidianRagEngine.Tests.Ocr.Helpers;
using ObsidianRagEngine.Tests.Setup;

namespace ObsidianRagEngine.Tests.Ocr;

public class MessengerOcrServiceTests(OcrFixture fixture) : IClassFixture<OcrFixture>
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
        
        var sut = fixture.MessengerScreenshot;
        fixture.Settings.ResetOcrResultFolder(testCase, sut.ModelName);

        // Act
        var ocredText = await sut.ExtractText(
            imageBytes,
            [TesseractLanguages.Russian, TesseractLanguages.English],
            CancellationToken.None,
            new MessengerOcrCallbacks
            {
                OnPanelOcr = (raw, normalized, text) =>
                    fixture.Settings.SavePanelOcrResult(testCase, sut.ModelName, raw, normalized, text)
            });

        var score = TextComparer.Compare(ocredText, testCase.ExpectedText);

        fixture.Settings.SaveOcrResult(testCase, sut.ModelName, ocredText, score);

        // Assert
        score.Should().BeGreaterThanOrEqualTo(MinimumSimilarity);
    }
}
