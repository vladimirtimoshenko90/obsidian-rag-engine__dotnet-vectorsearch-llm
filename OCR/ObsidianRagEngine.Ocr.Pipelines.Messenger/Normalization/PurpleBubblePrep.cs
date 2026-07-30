using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace ObsidianRagEngine.Ocr.Pipelines.Messenger.Normalization;

/// <summary>
/// Detects and rewrites purple/violet messenger bubbles for OCR (dark or light theme).
/// </summary>
internal static class PurpleBubblePrep
{
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

    /// <summary>
    /// Dark-theme prep: sizable purple regions → boost light text to white, purple fill to black
    /// (invert later yields dark-on-light).
    /// </summary>
    public static void PrepDarkTheme(Image<Rgba32> image)
    {
        foreach (var region in FindAcceptedPurpleRegions(image))
            RewritePurpleRegion(image, region, lightTextToWhite: true);
    }

    /// <summary>
    /// Light-theme prep: sizable purple regions → near-white text to black,
    /// then purple fill to white (already document-style, no invert).
    /// </summary>
    public static void PrepLightTheme(Image<Rgba32> image)
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
        purple = BinaryMaskMorphology.Close(purple, w, h);

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
                    var lum = PanelImageOps.ToGray(p);

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
        if (PanelImageOps.ToGray(p) >= PurpleFillLuminanceMax)
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
}
