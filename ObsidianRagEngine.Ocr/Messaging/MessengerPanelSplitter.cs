using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace ObsidianRagEngine.Ocr.Messaging;

/// <summary>
/// Splits a side-by-side messenger screenshot composite into individual panel images
/// by detecting narrow vertical gutters (seams).
/// </summary>
public sealed class MessengerPanelSplitter
{
    private const int SmoothWindow = 5;
    private const int MinGutterWidth = 1;
    private const int MaxGutterWidth = 10;
    private const int EdgeMarginPx = 8;
    private const float MinPanelWidthFraction = 0.12f;
    private const float BusyQuietFraction = 0.35f;

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
        var cuts = FindCutPositions(image);

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
        return FindCutPositions(image);
    }

    private static List<int> FindCutPositions(Image<Rgba32> image)
    {
        var width = image.Width;
        var height = image.Height;
        if (width < 32 || height < 32)
            return [0, width];

        var busy = new float[width];
        var brightness = new float[width];

        image.ProcessPixelRows(accessor =>
        {
            for (var y = 0; y < height; y++)
            {
                var row = accessor.GetRowSpan(y);
                Span<Rgba32> nextRow = default;
                var hasNext = y + 1 < height;
                if (hasNext)
                    nextRow = accessor.GetRowSpan(y + 1);

                for (var x = 0; x < width; x++)
                {
                    var g = ToGray(row[x]);
                    brightness[x] += g;
                    if (hasNext)
                        busy[x] += Math.Abs(g - ToGray(nextRow[x]));
                }
            }
        });

        var invH = 1f / height;
        var invHBusy = height > 1 ? 1f / (height - 1) : 1f;
        for (var x = 0; x < width; x++)
        {
            brightness[x] *= invH;
            busy[x] *= invHBusy;
        }

        NormalizeInPlace(busy);
        var gradient = new float[width];
        for (var x = 1; x < width - 1; x++)
            gradient[x] = Math.Abs(brightness[x + 1] - brightness[x - 1]) * 0.5f;
        gradient[0] = gradient[1];
        gradient[width - 1] = gradient[width - 2];
        NormalizeInPlace(gradient);

        var combined = new float[width];
        for (var x = 0; x < width; x++)
            combined[x] = busy[x] * 0.6f + (1f - gradient[x]) * 0.4f;

        var smoothed = MovingAverage(combined, SmoothWindow);

        var minPanelWidth = Math.Max(24, (int)(width * MinPanelWidthFraction));
        var gutters = FindGutterCenters(smoothed, busy, width, minPanelWidth);

        var cuts = new List<int> { 0 };
        cuts.AddRange(gutters);
        cuts.Add(width);
        return cuts;
    }

    private static List<int> FindGutterCenters(float[] score, float[] busy, int width, int minPanelWidth)
    {
        var busyMean = busy.Average();
        var busyQuiet = Math.Max(0.05f, busyMean * BusyQuietFraction);

        var candidateRuns = new List<(int Start, int End)>();
        var inRun = false;
        var runStart = 0;

        for (var x = EdgeMarginPx; x < width - EdgeMarginPx; x++)
        {
            var quiet = busy[x] <= busyQuiet;
            if (quiet && !inRun)
            {
                inRun = true;
                runStart = x;
            }
            else if (!quiet && inRun)
            {
                inRun = false;
                MaybeAddRun(candidateRuns, runStart, x - 1);
            }
        }

        if (inRun)
            MaybeAddRun(candidateRuns, runStart, width - EdgeMarginPx - 1);

        // Prefer local minima of combined score within each narrow quiet run
        var centers = new List<int>();
        foreach (var (start, end) in candidateRuns)
        {
            var bestX = start;
            var bestScore = score[start];
            for (var x = start; x <= end; x++)
            {
                if (score[x] < bestScore)
                {
                    bestScore = score[x];
                    bestX = x;
                }
            }

            centers.Add(bestX);
        }

        // Enforce minimum panel spacing: keep lowest-score gutters when too close
        centers.Sort();
        var filtered = new List<int>();
        foreach (var c in centers)
        {
            if (filtered.Count == 0)
            {
                if (c >= minPanelWidth && width - c >= minPanelWidth)
                    filtered.Add(c);
                continue;
            }

            var prev = filtered[^1];
            if (c - prev < minPanelWidth)
            {
                // Keep the quieter (lower score) of the two
                if (score[c] < score[prev])
                    filtered[^1] = c;
                continue;
            }

            if (width - c >= minPanelWidth)
                filtered.Add(c);
        }

        return filtered;

        static void MaybeAddRun(List<(int Start, int End)> runs, int start, int end)
        {
            var w = end - start + 1;
            if (w is >= MinGutterWidth and <= MaxGutterWidth)
                runs.Add((start, end));
        }
    }

    private static void NormalizeInPlace(float[] values)
    {
        var min = values.Min();
        var max = values.Max();
        var range = max - min;
        if (range < 1e-6f)
        {
            Array.Fill(values, 0.5f);
            return;
        }

        for (var i = 0; i < values.Length; i++)
            values[i] = (values[i] - min) / range;
    }

    private static float[] MovingAverage(float[] values, int window)
    {
        var half = window / 2;
        var result = new float[values.Length];
        for (var i = 0; i < values.Length; i++)
        {
            var from = Math.Max(0, i - half);
            var to = Math.Min(values.Length - 1, i + half);
            float sum = 0;
            for (var j = from; j <= to; j++)
                sum += values[j];
            result[i] = sum / (to - from + 1);
        }

        return result;
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
