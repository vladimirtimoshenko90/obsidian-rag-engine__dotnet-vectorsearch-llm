using System.ClientModel;

namespace ObsidianRagEngine.Llm.Kimi;

public sealed class KimiException : Exception
{
    private const int MaxErrorBodyChars = 512;

    public int? Status { get; }

    public KimiException(string message, int? status = null, Exception? innerException = null)
        : base(message, innerException)
    {
        Status = status;
    }

    public static KimiException FromComplete(ClientResultException ex)
    {
        ArgumentNullException.ThrowIfNull(ex);

        var detail = Truncate(ex.GetRawResponse()?.Content?.ToString() ?? ex.Message);
        return new KimiException(
            $"Kimi chat completion failed (HTTP {ex.Status}): {detail}",
            ex.Status,
            ex);
    }

    private static string Truncate(string value)
    {
        if (string.IsNullOrEmpty(value) || value.Length <= MaxErrorBodyChars)
            return value;

        return value[..MaxErrorBodyChars] + "…";
    }
}
