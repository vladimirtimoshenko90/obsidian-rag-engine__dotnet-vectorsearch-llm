using FluentAssertions;
using ObsidianRagEngine.Contracts;
using ObsidianRagEngine.Ocr.Pipelines.Messenger;
using ObsidianRagEngine.Tests.Ocr.Helpers;
using ObsidianRagEngine.Tests.Setup;

namespace ObsidianRagEngine.Tests.Ocr;

public class MessengerOcrServiceTests(OcrFixture fixture) : IClassFixture<OcrFixture>
{
    private const double MinimumSimilarity = 0.6;

    public static IEnumerable<object[]> TheoryCases()
    {
        foreach (var testCase in OcrTestStore.AllTestCases)
            foreach (var llm in LlmProviders.All)
                yield return [testCase, llm];
    }

    [Theory]
    [MemberData(nameof(TheoryCases))]
    public async Task ExtractText_FromSampleImage_MatchesExpectedText(
        OcrTestCase testCase, ILlmProvider llm)
    {
        // Arrange
        var imageBytes = await File.ReadAllBytesAsync(testCase.ImagePath);
        var sut = new MessengerOcrService(fixture.Tesseract, llm);
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
