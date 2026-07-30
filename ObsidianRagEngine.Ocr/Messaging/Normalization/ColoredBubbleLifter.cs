using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace ObsidianRagEngine.Ocr.Messaging.Normalization;

/// <summary>
/// Maps remaining saturated bubbles toward document style: colored fill → near-white;
/// light enclosed regions (glyphs) → near-black.
/// </summary>
internal static class ColoredBubbleLifter
{
    /// <summary>Min chroma to treat a pixel as part of a colored bubble.</summary>
    private const float BubbleChromaMin = 0.12f;

    /// <summary>Light pixels on a colored bubble above this luminance become text (dark).</summary>
    private const float BubbleTextLuminanceMin = 0.55f;

    public static void Lift(Image<Rgba32> image)
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
                    lum[i] = PanelImageOps.ToGray(p);
                    isBubble[i] = IsColoredBubbleFill(p, lum[i]);
                }
            }
        });

        // Seal 1px anti-alias gaps so white text stays enclosed by the bubble.
        isBubble = BinaryMaskMorphology.Close(isBubble, w, h);

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
}
