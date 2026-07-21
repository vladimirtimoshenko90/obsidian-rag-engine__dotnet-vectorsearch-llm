using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace ObsidianRagEngine.Ocr.Messaging;

/// <summary>
/// Prepares a single messenger panel for Tesseract: dark text on a light background,
/// with colored bubble (e.g. white-on-purple) lift. Does not crop header chrome.
/// </summary>
public sealed class MessengerPanelNormalizer
{
    /// <summary>Mean luminance below this (0…1) → treat panel as dark UI and invert.</summary>
    private const float DarkLuminanceThreshold = 0.45f;

    /// <summary>Min chroma to treat a pixel as part of a colored bubble.</summary>
    private const float BubbleChromaMin = 0.12f;

    /// <summary>Light pixels on a colored bubble above this luminance become text (dark).</summary>
    private const float BubbleTextLuminanceMin = 0.55f;

    private const float DarkContrast = 1.2f;
    private const float LightContrast = 1.35f;

    /// <summary>
    /// Returns a normalized PNG of <paramref name="panelBytes"/> suitable for document OCR.
    /// </summary>
    public byte[] Normalize(byte[] panelBytes)
    {
        ArgumentNullException.ThrowIfNull(panelBytes);
        if (panelBytes.Length == 0)
            throw new ArgumentException("Panel bytes are empty.", nameof(panelBytes));

        using var image = Image.Load<Rgba32>(panelBytes);
        var avg = AverageLuminance(image);
        var isDark = avg < DarkLuminanceThreshold;

        if (isDark)
            image.Mutate(ctx => ctx.Invert());

        // After invert (if any), lift remaining white/light text on saturated bubbles.
        LiftColoredBubbles(image);

        image.Mutate(ctx =>
        {
            ctx.Grayscale();
            ctx.Contrast(isDark ? DarkContrast : LightContrast);
        });

        return EncodePng(image);
    }

    private static float AverageLuminance(Image<Rgba32> image)
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

    /// <summary>
    /// Maps saturated bubbles toward document style: colored fill → near-white;
    /// light regions enclosed by that fill (white-on-purple glyphs) → near-black.
    /// </summary>
    private static void LiftColoredBubbles(Image<Rgba32> image)
    {
        var w = image.Width;
        var h = image.Height;
        var lum = new float[w * h];
        var isBubble = new bool[w * h];

        image.ProcessPixelRows(accessor =>
        {
            for (var y = 0; y < h; y++)
            {
                var row = accessor.GetRowSpan(y);
                for (var x = 0; x < w; x++)
                {
                    var i = y * w + x;
                    var p = row[x];
                    lum[i] = ToGray(p);
                    isBubble[i] = IsColoredBubbleFill(p, lum[i]);
                }
            }
        });

        // Seal 1px anti-alias gaps so white text stays enclosed by the bubble.
        isBubble = CloseMask(isBubble, w, h);

        var enclosed = FindEnclosedPixels(isBubble, w, h);

        image.ProcessPixelRows(accessor =>
        {
            for (var y = 0; y < h; y++)
            {
                var row = accessor.GetRowSpan(y);
                for (var x = 0; x < w; x++)
                {
                    var i = y * w + x;
                    var a = row[x].A;
                    if (isBubble[i])
                        row[x] = new Rgba32(245, 245, 245, a);
                    else if (enclosed[i] && lum[i] >= BubbleTextLuminanceMin)
                        row[x] = new Rgba32(20, 20, 20, a);
                }
            }
        });
    }

    private static bool IsColoredBubbleFill(Rgba32 p, float luminance)
    {
        var r = p.R / 255f;
        var g = p.G / 255f;
        var b = p.B / 255f;
        var max = Math.Max(r, Math.Max(g, b));
        var min = Math.Min(r, Math.Min(g, b));
        return max - min >= BubbleChromaMin && max >= 0.25f && luminance < BubbleTextLuminanceMin;
    }

    /// <summary>Morphological close (dilate then erode) with 1px radius.</summary>
    private static bool[] CloseMask(bool[] mask, int w, int h) =>
        Erode(Dilate(mask, w, h), w, h);

    private static bool[] Dilate(bool[] mask, int w, int h)
    {
        var result = new bool[mask.Length];
        for (var y = 0; y < h; y++)
        for (var x = 0; x < w; x++)
        {
            if (!mask[y * w + x])
                continue;
            for (var dy = -1; dy <= 1; dy++)
            for (var dx = -1; dx <= 1; dx++)
            {
                var xx = x + dx;
                var yy = y + dy;
                if ((uint)xx < (uint)w && (uint)yy < (uint)h)
                    result[yy * w + xx] = true;
            }
        }

        return result;
    }

    private static bool[] Erode(bool[] mask, int w, int h)
    {
        var result = new bool[mask.Length];
        for (var y = 0; y < h; y++)
        for (var x = 0; x < w; x++)
        {
            var ok = true;
            for (var dy = -1; dy <= 1 && ok; dy++)
            for (var dx = -1; dx <= 1; dx++)
            {
                var xx = x + dx;
                var yy = y + dy;
                if ((uint)xx >= (uint)w || (uint)yy >= (uint)h || !mask[yy * w + xx])
                {
                    ok = false;
                    break;
                }
            }

            result[y * w + x] = ok;
        }

        return result;
    }

    /// <summary>
    /// Pixels not part of the bubble mask and not reachable from the image border
    /// through non-bubble pixels — i.e. holes inside bubbles (typically white text).
    /// </summary>
    private static bool[] FindEnclosedPixels(bool[] isBubble, int w, int h)
    {
        var reachableFromBorder = new bool[isBubble.Length];
        var queue = new Queue<int>();

        void TryEnqueue(int x, int y)
        {
            if ((uint)x >= (uint)w || (uint)y >= (uint)h)
                return;
            var i = y * w + x;
            if (isBubble[i] || reachableFromBorder[i])
                return;
            reachableFromBorder[i] = true;
            queue.Enqueue(i);
        }

        for (var x = 0; x < w; x++)
        {
            TryEnqueue(x, 0);
            TryEnqueue(x, h - 1);
        }

        for (var y = 0; y < h; y++)
        {
            TryEnqueue(0, y);
            TryEnqueue(w - 1, y);
        }

        while (queue.Count > 0)
        {
            var i = queue.Dequeue();
            var x = i % w;
            var y = i / w;
            TryEnqueue(x - 1, y);
            TryEnqueue(x + 1, y);
            TryEnqueue(x, y - 1);
            TryEnqueue(x, y + 1);
        }

        var enclosed = new bool[isBubble.Length];
        for (var i = 0; i < enclosed.Length; i++)
            enclosed[i] = !isBubble[i] && !reachableFromBorder[i];
        return enclosed;
    }

    private static float ToGray(Rgba32 p) =>
        (0.299f * p.R + 0.587f * p.G + 0.114f * p.B) / 255f;

    private static byte[] EncodePng(Image<Rgba32> image)
    {
        using var ms = new MemoryStream();
        image.SaveAsPng(ms);
        return ms.ToArray();
    }
}
