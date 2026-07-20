using FluentAssertions;
using ObsidianRagEngine.Console.Common.Extensions;
using ObsidianRagEngine.Console.Domain.Ocr;
using ObsidianRagEngine.Tests.Setup;

namespace ObsidianRagEngine.Tests.Domain.Ocr;

public class TesseractOllamaServiceTests(TesseractOllamaFixture fixture) : IClassFixture<TesseractOllamaFixture>
{
    [Fact]
    public async Task ExtractText_FromSampleChatScreenshot_ReturnsNonEmptyText()
    {
        var imageBytes = await File.ReadAllBytesAsync(fixture.Settings.OcrSampleImagePath);

        var text = await fixture.Sut.ExtractText(
            imageBytes,
            [TesseractLanguages.Russian, TesseractLanguages.English]);

        text.Valuable().Should().BeTrue();
    }
}
