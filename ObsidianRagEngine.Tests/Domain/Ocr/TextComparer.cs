using System.Buffers;
using System.Globalization;
using System.Text;

namespace ObsidianRagEngine.Tests.Domain.Ocr;

/// <summary>
/// Normalized Levenshtein similarity in [0, 1] after stripping punctuation/emoji/symbols
/// and collapsing whitespace (including line breaks).
/// </summary>
public static class TextComparer
{
    public static double Compare(string? actual, string? expected)
    {
        var left = Normalize(actual);
        var right = Normalize(expected);

        if (left.Length == 0 && right.Length == 0)
            return 1.0;

        if (left.Length == 0 || right.Length == 0)
            return 0.0;

        if (left.Length > right.Length)
            (left, right) = (right, left);

        var distance = LevenshteinDistance(left, right);
        return 1.0 - (double)distance / right.Length;
    }

    private static string Normalize(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return string.Empty;

        var builder = new StringBuilder(value.Length);
        var pendingSpace = false;

        foreach (var rune in value.EnumerateRunes())
        {
            if (Rune.IsWhiteSpace(rune))
            {
                pendingSpace = builder.Length > 0;
                continue;
            }

            if (!Rune.IsLetter(rune) && !Rune.IsDigit(rune))
                continue;

            if (pendingSpace)
            {
                builder.Append(' ');
                pendingSpace = false;
            }

            builder.Append(Rune.ToLower(rune, CultureInfo.InvariantCulture));
        }

        return builder.ToString();
    }

    private static int LevenshteinDistance(string shorter, string longer)
    {
        var n = shorter.Length;
        var m = longer.Length;

        var prev = ArrayPool<int>.Shared.Rent(n + 1);
        var curr = ArrayPool<int>.Shared.Rent(n + 1);

        try
        {
            for (var i = 0; i <= n; i++)
                prev[i] = i;

            for (var j = 1; j <= m; j++)
            {
                curr[0] = j;
                var longerChar = longer[j - 1];

                for (var i = 1; i <= n; i++)
                {
                    var cost = shorter[i - 1] == longerChar ? 0 : 1;
                    curr[i] = Min(curr[i - 1] + 1, prev[i] + 1, prev[i - 1] + cost);
                }

                (prev, curr) = (curr, prev);
            }

            return prev[n];
        }
        finally
        {
            ArrayPool<int>.Shared.Return(prev);
            ArrayPool<int>.Shared.Return(curr);
        }
    }

    private static int Min(int a, int b, int c) => Math.Min(a, Math.Min(b, c));
}
