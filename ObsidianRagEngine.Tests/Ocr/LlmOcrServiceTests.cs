using FluentAssertions;
using ObsidianRagEngine.Contracts;
using ObsidianRagEngine.Tests.Ocr.Helpers;
using ObsidianRagEngine.Tests.Setup;

namespace ObsidianRagEngine.Tests.Ocr;

/// <summary>
/// Direct vision <see cref="IOcrProvider.ExtractText"/> coverage (Kimi, Alibaba).
/// </summary>
public class LlmOcrServiceTests(OcrFixture fixture) : IClassFixture<OcrFixture>
{
    private const double MinimumSimilarity = 0.2;   // minimal accuracy is enough, tests are just checking that "something is detected" and ocr does not fail

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

        var sut = fixture.GetOcrProvider(llmSpec)!;
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
