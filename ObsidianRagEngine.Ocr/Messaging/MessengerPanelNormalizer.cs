using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace ObsidianRagEngine.Ocr.Messaging;

/// <summary>
/// Prepares a single messenger panel for Tesseract as a short pipeline:
/// optional 2× upscale (small dark crops) → purple bubble prep → invert (dark) →
/// lift remaining colored bubbles → grayscale/contrast.
/// Does not crop header chrome.
/// </summary>
public sealed class MessengerPanelNormalizer
{
    /// <summary>Mean luminance below this (0…1) → treat panel as dark UI and invert.</summary>
    private const float DarkLuminanceThreshold = 0.45f;

    /// <summary>Min chroma to treat a pixel as part of a colored bubble.</summary>
    private const float BubbleChromaMin = 0.12f;

    /// <summary>Light pixels on a colored bubble above this luminance become text (dark).</summary>
    private const float BubbleTextLuminanceMin = 0.55f;

    /// <summary>
    /// HSV hue band (degrees) for violet / purple / magenta / blue-violet bubble fills.
    /// Lower bound stays above typical UI blues (~210°); includes bluer bottom bubbles (~225–235°).
    /// </summary>
    private const float PurpleHueMinDeg = 225f;
    private const float PurpleHueMaxDeg = 310f;

    /// <summary>Min HSV saturation so gray wallpaper / chrome is left alone.</summary>
    private const float PurpleSaturationMin = 0.22f;

    /// <summary>
    /// Purple-like pixels at or above this luminance are glyph AA / light text, not fill.
    /// </summary>
    private const float PurpleFillLuminanceMax = 0.62f;

    /// <summary>Dark-theme bubble interiors: half-light glyphs forced to pure white before blacken.</summary>
    private const float DarkThemeLightTextMin = 0.55f;

    /// <summary>
    /// Light-theme bubble interiors: light / near-white glyphs (incl. AA) forced to black.
    /// Kept above purple-fill luminance so fill is not blackened.
    /// </summary>
    private const float LightThemeWhiteTextMin = 0.65f;

    /// <summary>Min purple-pixel density inside a candidate bubble bounding box.</summary>
    private const float PurpleRegionDensityMin = 0.30f;

    /// <summary>Ignore tiny purple blobs (wallpaper / AA); allow short one-line bubbles.</summary>
    private const int MinPurpleRegionWidth = 40;
    private const int MinPurpleRegionHeight = 20;

    private const float DarkContrast = 1.2f;
    private const float LightContrast = 1.35f;

    /// <summary>
    /// Upscale only very small dark phone crops. Larger / light panels are already
    /// sharp enough; 2× there tends to fatten strokes and nudge scores down.
    /// </summary>
    private const int UpscaleMinShortSidePx = 350;

    /// <summary>Lanczos upscale factor for qualifying panels.</summary>
    private const float UpscaleFactor = 2f;

    /// <summary>
    /// Returns a normalized PNG of <paramref name="panelBytes"/> suitable for document OCR.
    /// Pipeline: optional upscale → purple prep → invert (dark) → lift bubbles → grayscale/contrast.
    /// </summary>
    public byte[] Normalize(byte[] panelBytes)
    {
        ArgumentNullException.ThrowIfNull(panelBytes);
        if (panelBytes.Length == 0)
            throw new ArgumentException("Panel bytes are empty.", nameof(panelBytes));

        using var image = Image.Load<Rgba32>(panelBytes);
        var isDark = AverageLuminance(image) < DarkLuminanceThreshold;

        // Small dark crops only — denser glyphs before purple prep / invert.
        UpscaleIfSmallDark(image, isDark);

        if (isDark)
            PrepDarkThemePurpleBubbles(image);
        else
            PrepLightThemePurpleBubbles(image);

        if (isDark)
            Invert(image);

        LiftColoredBubbles(image);
        ApplyGrayscaleContrast(image, isDark);

        return EncodePng(image);
    }

    private static void Invert(Image<Rgba32> image) =>
        image.Mutate(ctx => ctx.Invert());

    private static void ApplyGrayscaleContrast(Image<Rgba32> image, bool isDark)
    {
        image.Mutate(ctx =>
        {
            ctx.Grayscale();
            ctx.Contrast(isDark ? DarkContrast : LightContrast);
        });
    }

    /// <summary>
    /// First pipeline step when useful: 2× Lanczos for small dark panels only.
    /// </summary>
    private static void UpscaleIfSmallDark(Image<Rgba32> image, bool isDark)
    {
        if (!isDark)
            return;

        var shortSide = Math.Min(image.Width, image.Height);
        if (shortSide >= UpscaleMinShortSidePx)
            return;

        var width = Math.Max(1, (int)Math.Round(image.Width * UpscaleFactor));
        var height = Math.Max(1, (int)Math.Round(image.Height * UpscaleFactor));
        image.Mutate(ctx => ctx.Resize(width, height, KnownResamplers.Lanczos3));
    }

    /// <summary>
    /// Dark-theme prep: sizable purple regions → boost light text to white, purple fill to black
    /// (invert later yields dark-on-light).
    /// </summary>
    private static void PrepDarkThemePurpleBubbles(Image<Rgba32> image)
    {
        foreach (var region in FindAcceptedPurpleRegions(image))
            RewritePurpleRegion(image, region, lightTextToWhite: true);
    }

    /// <summary>
    /// Light-theme prep: sizable purple regions → near-white text to black,
    /// then purple fill to white (already document-style, no invert).
    /// </summary>
    private static void PrepLightThemePurpleBubbles(Image<Rgba32> image)
    {
        foreach (var region in FindAcceptedPurpleRegions(image))
            RewritePurpleRegion(image, region, lightTextToWhite: false);
    }

    private static List<PurpleRegion> FindAcceptedPurpleRegions(Image<Rgba32> image)
    {
        var w = image.Width;
        var h = image.Height;
        var purple = new bool[w * h];

        image.ProcessPixelRows(accessor =>
        {
            for (var y = 0; y < h; y++)
            {
                var row = accessor.GetRowSpan(y);
                for (var x = 0; x < w; x++)
                    purple[y * w + x] = IsPurpleLikeFill(row[x]);
            }
        });

        // Seal 1px AA gaps so each bubble tends to be one connected component.
        purple = CloseMask(purple, w, h);

        var accepted = new List<PurpleRegion>();
        foreach (var region in FindPurpleRegions(purple, w, h))
        {
            if (region.Width < MinPurpleRegionWidth || region.Height < MinPurpleRegionHeight)
                continue;

            var area = region.Width * region.Height;
            if (area == 0 || region.PurpleCount / (float)area < PurpleRegionDensityMin)
                continue;

            accepted.Add(region);
        }

        return accepted;
    }

    /// <summary>
    /// Connected components of the purple mask → axis-aligned bounding boxes with counts.
    /// </summary>
    private static List<PurpleRegion> FindPurpleRegions(bool[] purple, int w, int h)
    {
        var visited = new bool[purple.Length];
        var regions = new List<PurpleRegion>();
        var queue = new Queue<int>();

        for (var i = 0; i < purple.Length; i++)
        {
            if (!purple[i] || visited[i])
                continue;

            var minX = i % w;
            var maxX = minX;
            var minY = i / w;
            var maxY = minY;
            var count = 0;

            visited[i] = true;
            queue.Enqueue(i);

            while (queue.Count > 0)
            {
                var cur = queue.Dequeue();
                var x = cur % w;
                var y = cur / w;
                count++;
                if (x < minX) minX = x;
                if (x > maxX) maxX = x;
                if (y < minY) minY = y;
                if (y > maxY) maxY = y;

                TryEnqueue(x - 1, y);
                TryEnqueue(x + 1, y);
                TryEnqueue(x, y - 1);
                TryEnqueue(x, y + 1);
            }

            regions.Add(new PurpleRegion(minX, minY, maxX, maxY, count));

            void TryEnqueue(int x, int y)
            {
                if ((uint)x >= (uint)w || (uint)y >= (uint)h)
                    return;
                var ni = y * w + x;
                if (!purple[ni] || visited[ni])
                    return;
                visited[ni] = true;
                queue.Enqueue(ni);
            }
        }

        return regions;
    }

    /// <summary>
    /// Inside a purple bubble box: rewrite light glyphs, then rewrite purple fill.
    /// Dark theme: light→white, purple→black. Light theme: light→black, purple→white.
    /// </summary>
    private static void RewritePurpleRegion(Image<Rgba32> image, PurpleRegion region, bool lightTextToWhite)
    {
        var textLumMin = lightTextToWhite ? DarkThemeLightTextMin : LightThemeWhiteTextMin;
        var textRgb = lightTextToWhite ? (byte)255 : (byte)0;
        var fillRgb = lightTextToWhite ? (byte)0 : (byte)255;

        image.ProcessPixelRows(accessor =>
        {
            for (var y = region.MinY; y <= region.MaxY; y++)
            {
                var row = accessor.GetRowSpan(y);
                for (var x = region.MinX; x <= region.MaxX; x++)
                {
                    var p = row[x];
                    var a = p.A;
                    var lum = ToGray(p);

                    if (lum >= textLumMin)
                    {
                        row[x] = new Rgba32(textRgb, textRgb, textRgb, a);
                        continue;
                    }

                    if (IsPurpleHue(p))
                        row[x] = new Rgba32(fillRgb, fillRgb, fillRgb, a);
                }
            }
        });
    }

    private static bool IsPurpleLikeFill(Rgba32 p)
    {
        if (ToGray(p) >= PurpleFillLuminanceMax)
            return false;

        return IsPurpleHue(p);
    }

    /// <summary>Purple / violet / magenta by HSV hue + saturation (no luminance gate).</summary>
    private static bool IsPurpleHue(Rgba32 p)
    {
        var r = p.R / 255f;
        var g = p.G / 255f;
        var b = p.B / 255f;
        var max = Math.Max(r, Math.Max(g, b));
        var min = Math.Min(r, Math.Min(g, b));
        var chroma = max - min;
        if (chroma < 1e-6f)
            return false;

        var saturation = chroma / max;
        if (saturation < PurpleSaturationMin)
            return false;

        float hueDeg;
        if (Math.Abs(max - r) < 1e-6f)
            hueDeg = 60f * (((g - b) / chroma) + 6f);
        else if (Math.Abs(max - g) < 1e-6f)
            hueDeg = 60f * (((b - r) / chroma) + 2f);
        else
            hueDeg = 60f * (((r - g) / chroma) + 4f);

        hueDeg %= 360f;
        if (hueDeg < 0f)
            hueDeg += 360f;

        return hueDeg >= PurpleHueMinDeg && hueDeg <= PurpleHueMaxDeg;
    }

    private readonly record struct PurpleRegion(int MinX, int MinY, int MaxX, int MaxY, int PurpleCount)
    {
        public int Width => MaxX - MinX + 1;
        public int Height => MaxY - MinY + 1;
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
