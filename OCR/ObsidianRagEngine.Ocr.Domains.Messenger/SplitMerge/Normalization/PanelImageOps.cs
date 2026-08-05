using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace ObsidianRagEngine.Ocr.Domains.Messenger.SplitMerge.Normalization;

/// <summary>
/// Shared ImageSharp helpers for panel normalization.
/// </summary>
internal static class PanelImageOps
{
    public static float ToGray(Rgba32 p) =>
        (0.299f * p.R + 0.587f * p.G + 0.114f * p.B) / 255f;

    public static byte[] EncodePng(Image<Rgba32> image)
    {
        using var ms = new MemoryStream();
        image.SaveAsPng(ms);
        return ms.ToArray();
    }

    public static float AverageLuminance(Image<Rgba32> image)
    {
        double sum = 0;
        var count = image.Width * image.Height;
        image.ProcessPixelRows(accessor =>
        {
            for (var y = 0; y < accessor.Height; y++)
            {
                var row = accessor.GetRowSpan(y);
                for (var x = 0; x < row.Length; x++)
                    sum += ToGray(row[x]);
            }
        });

        return count == 0 ? 0.5f : (float)(sum / count);
    }

    public static void Invert(Image<Rgba32> image) =>
        image.Mutate(ctx => ctx.Invert());

    public static void ApplyGrayscaleContrast(Image<Rgba32> image, float contrast) =>
        image.Mutate(ctx =>
        {
            ctx.Grayscale();
            ctx.Contrast(contrast);
        });

    public static void UpscaleIfNeeded(Image<Rgba32> image, int minShortSidePx, float factor)
    {
        var shortSide = Math.Min(image.Width, image.Height);
        if (shortSide >= minShortSidePx)
            return;

        var width = Math.Max(1, (int)Math.Round(image.Width * factor));
        var height = Math.Max(1, (int)Math.Round(image.Height * factor));
        image.Mutate(ctx => ctx.Resize(width, height, KnownResamplers.Lanczos3));
    }
}
