namespace ObsidianRagEngine.Console.Common.Extensions;

public static class StringExtensions
{
    public static bool Valuable(this string? value) => !string.IsNullOrWhiteSpace(value);
}
