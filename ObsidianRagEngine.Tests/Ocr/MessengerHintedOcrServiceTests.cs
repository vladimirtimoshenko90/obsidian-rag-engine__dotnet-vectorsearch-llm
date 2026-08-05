using FluentAssertions;
using ObsidianRagEngine.Contracts;
using ObsidianRagEngine.Ocr.Pipelines.Messenger.Hinted;
using ObsidianRagEngine.Tests.Ocr.Helpers;
using ObsidianRagEngine.Tests.Setup;

namespace ObsidianRagEngine.Tests.Ocr;

/// <summary>
/// Full-image vision OCR with messenger clarification via <see cref="MessengerHintedOcrService"/>.
/// </summary>
public class MessengerHintedOcrServiceTests(OcrFixture fixture) : IClassFixture<OcrFixture>
{
    private const double MinimumSimilarity = 0.4;

    public static IEnumerable<object[]> TheoryCases()
    {
        foreach (var testCase in OcrTestStore.AllTestCases)
            foreach (var llmSpec in LlmProviders.OcrCapable)
                yield return [testCase, llmSpec];
    }

    [Theory]
    [MemberData(nameof(TheoryCases))]
    public async Task ExtractText_FromSampleImage_MatchesExpectedText(
        OcrTestCase testCase, LlmProviderSpec llmSpec)
    {
        // Arrange
        var imageBytes = await File.ReadAllBytesAsync(testCase.ImagePath);

        var ocrProvider = fixture.GetOcrProvider(llmSpec)!;
        var sut = new MessengerHintedOcrService(ocrProvider);
        OcrTestStore.ResetResultFolder(testCase, sut.ModelName);

        // Act
        var ocrResult = await sut.ExtractText(
            imageBytes,
            [OcrLanguage.Russian, OcrLanguage.English],
            CancellationToken.None);

        var score = TextComparer.Compare(ocrResult.Text, testCase.ExpectedText);

        OcrTestStore.SaveResult(testCase, sut.ModelName, ocrResult, score);

        // Assert
        score.Should().BeGreaterThanOrEqualTo(MinimumSimilarity);
    }
}
