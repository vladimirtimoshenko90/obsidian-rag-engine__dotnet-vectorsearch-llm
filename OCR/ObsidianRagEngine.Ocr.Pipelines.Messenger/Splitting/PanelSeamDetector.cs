using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace ObsidianRagEngine.Ocr.Pipelines.Messenger.Splitting;

/// <summary>
/// Finds vertical cut positions in a side-by-side messenger composite
/// by detecting low-contrast gutters (skipping header/footer chrome).
/// </summary>
internal static class PanelSeamDetector
{
    private const float TopSkipFraction = 0.20f;
    private const float BottomSkipFraction = 0.05f;
    private const float TailPercentile = 0.05f;
    private const int SmoothWindow = 5;
    private const int EdgeMarginPx = 8;
    private const float MinPanelWidthFraction = 0.12f;

    /// <summary>
    /// Returns cut X positions including 0 and Width.
    /// </summary>
    public static List<int> FindCutPositions(Image<Rgba32> image)
    {
        var width = image.Width;
        var height = image.Height;
        if (width < 32 || height < 32)
            return [0, width];

        var yFrom = (int)(height * TopSkipFraction);
        var yTo = (int)(height * (1f - BottomSkipFraction));
        if (yTo - yFrom < 16)
            return [0, width];

        var contrast = ComputeColumnContrast(image, yFrom, yTo);
        var smoothed = MovingAverage(contrast, SmoothWindow);

        var minPanelWidth = Math.Max(24, (int)(width * MinPanelWidthFraction));
        var gutters = FindGutterCenters(smoothed, width, minPanelWidth);

        var cuts = new List<int> { 0 };
        cuts.AddRange(gutters);
        cuts.Add(width);
        return cuts;
    }

    /// <summary>
    /// Per column: contrast between the lightest 5% and darkest 5% of pixels
    /// in the vertical band (header/footer already excluded).
    /// </summary>
    private static float[] ComputeColumnContrast(Image<Rgba32> image, int yFrom, int yTo)
    {
        var width = image.Width;
        var bandHeight = yTo - yFrom;
        var bands = new float[width][];
        for (var x = 0; x < width; x++)
            bands[x] = new float[bandHeight];

        image.ProcessPixelRows(accessor =>
        {
            for (var y = yFrom; y < yTo; y++)
            {
                var row = accessor.GetRowSpan(y);
                var yi = y - yFrom;
                for (var x = 0; x < width; x++)
                    bands[x][yi] = ToGray(row[x]);
            }
        });

        var contrast = new float[width];
        var tailCount = Math.Max(1, (int)(bandHeight * TailPercentile));
        for (var x = 0; x < width; x++)
        {
            var column = bands[x];
            Array.Sort(column);

            float darkSum = 0;
            float lightSum = 0;
            for (var t = 0; t < tailCount; t++)
            {
                darkSum += column[t];
                lightSum += column[bandHeight - 1 - t];
            }

            contrast[x] = (lightSum - darkSum) / tailCount;
        }

        return contrast;
    }

    private static List<int> FindGutterCenters(float[] contrast, int width, int minPanelWidth)
    {
        // Low-contrast runs are valley candidates (margins + seams). Pick the flattest
        // column in each run so wide plateaus still cut near the true border.
        var threshold = contrast.Average() * 0.45f;

        var runs = new List<(int Start, int End)>();
        var inRun = false;
        var runStart = 0;

        for (var x = EdgeMarginPx; x < width - EdgeMarginPx; x++)
        {
            var low = contrast[x] <= threshold;
            if (low && !inRun)
            {
                inRun = true;
                runStart = x;
            }
            else if (!low && inRun)
            {
                inRun = false;
                runs.Add((runStart, x - 1));
            }
        }

        if (inRun)
            runs.Add((runStart, width - EdgeMarginPx - 1));

        var centers = new List<int>();
        foreach (var (start, end) in runs)
        {
            var best = contrast[start];
            var bestFrom = start;
            var bestTo = start;
            for (var x = start; x <= end; x++)
            {
                if (contrast[x] < best)
                {
                    best = contrast[x];
                    bestFrom = x;
                    bestTo = x;
                }
                else if (contrast[x] == best)
                {
                    bestTo = x;
                }
            }

            centers.Add((bestFrom + bestTo) / 2);
        }

        centers.Sort();
        var filtered = new List<int>();
        foreach (var c in centers)
        {
            if (c < minPanelWidth || width - c < minPanelWidth)
                continue;

            if (filtered.Count == 0)
            {
                filtered.Add(c);
                continue;
            }

            var prev = filtered[^1];
            if (c - prev < minPanelWidth)
            {
                if (contrast[c] < contrast[prev])
                    filtered[^1] = c;
                continue;
            }

            filtered.Add(c);
        }

        return filtered;
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
}
