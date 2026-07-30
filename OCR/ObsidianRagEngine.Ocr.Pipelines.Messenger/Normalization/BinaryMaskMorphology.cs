namespace ObsidianRagEngine.Ocr.Pipelines.Messenger.Normalization;

/// <summary>
/// 1px-radius morphological close (dilate then erode) for binary masks.
/// </summary>
internal static class BinaryMaskMorphology
{
    public static bool[] Close(bool[] mask, int w, int h) =>
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
}
