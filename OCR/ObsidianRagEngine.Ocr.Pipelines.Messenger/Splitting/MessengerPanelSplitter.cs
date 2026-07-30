using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace ObsidianRagEngine.Ocr.Pipelines.Messenger.Splitting;

/// <summary>
/// Splits a side-by-side messenger screenshot composite into individual panel images
/// by detecting low-contrast vertical seams (skipping header/footer chrome).
/// </summary>
public sealed class MessengerPanelSplitter
{
    /// <summary>
    /// Crops <paramref name="imageBytes"/> into left-to-right panel PNGs.
    /// If no gutters are found, returns the original image as a single panel.
    /// </summary>
    public IReadOnlyList<byte[]> Split(byte[] imageBytes)
    {
        ArgumentNullException.ThrowIfNull(imageBytes);
        if (imageBytes.Length == 0)
            throw new ArgumentException("Image bytes are empty.", nameof(imageBytes));

        using var image = Image.Load<Rgba32>(imageBytes);
        var cuts = PanelSeamDetector.FindCutPositions(image);

        // No gutters → keep the original bytes (avoid re-encode).
        if (cuts.Count <= 2)
            return [imageBytes];

        var panels = new List<byte[]>(cuts.Count - 1);
        for (var i = 0; i < cuts.Count - 1; i++)
        {
            var x = cuts[i];
            var w = cuts[i + 1] - cuts[i];
            if (w <= 0)
                continue;

            using var panel = image.Clone(ctx => ctx.Crop(new Rectangle(x, 0, w, image.Height)));
            panels.Add(EncodePng(panel));
        }

        return panels.Count > 0 ? panels : [EncodePng(image)];
    }

    /// <summary>
    /// Returns cut X positions including 0 and Width (for diagnostics / tests).
    /// </summary>
    public IReadOnlyList<int> FindCutPositions(byte[] imageBytes)
    {
        ArgumentNullException.ThrowIfNull(imageBytes);
        using var image = Image.Load<Rgba32>(imageBytes);
        return PanelSeamDetector.FindCutPositions(image);
    }

    private static byte[] EncodePng(Image<Rgba32> image)
    {
        using var ms = new MemoryStream();
        image.SaveAsPng(ms);
        return ms.ToArray();
    }
}
