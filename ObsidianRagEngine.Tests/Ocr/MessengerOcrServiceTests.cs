using FluentAssertions;
using ObsidianRagEngine.Contracts;
using ObsidianRagEngine.Ocr.Messaging;
using ObsidianRagEngine.Tests.Ocr.Helpers;
using ObsidianRagEngine.Tests.Setup;

namespace ObsidianRagEngine.Tests.Ocr;

public class MessengerOcrServiceTests(OcrFixture fixture) : IClassFixture<OcrFixture>
{
    private const double MinimumSimilarity = 0.6;

    [Theory]
    [MemberData(nameof(OcrTestStore.TheoryCases), MemberType = typeof(OcrTestStore))]
    public async Task ExtractText_FromSampleImage_MatchesExpectedText(OcrTestCase testCase)
    {
        // Arrange
        var imageBytes = await File.ReadAllBytesAsync(testCase.ImagePath);

        var sut = fixture.MessengerScreenshot;
        OcrTestStore.ResetResultFolder(testCase, sut.ModelName);

        // Act
        var ocredText = await sut.ExtractText(
            imageBytes,
            [OcrLanguage.Russian, OcrLanguage.English],
            CancellationToken.None,
            new MessengerOcrCallbacks
            {
                OnPanelOcr = (raw, normalized, text) =>
                    OcrTestStore.SavePanelResult(testCase, sut.ModelName, raw, normalized, text)
            });

        var score = TextComparer.Compare(ocredText, testCase.ExpectedText);

        OcrTestStore.SaveResult(testCase, sut.ModelName, ocredText, score);

        // Assert
        score.Should().BeGreaterThanOrEqualTo(MinimumSimilarity);
    }
}
