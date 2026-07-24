using System.ClientModel;

namespace ObsidianRagEngine.Llm.DeepSeek;

public sealed class DeepSeekException : Exception
{
    private const int MaxErrorBodyChars = 512;

    public int? Status { get; }

    public DeepSeekException(string message, int? status = null, Exception? innerException = null)
        : base(message, innerException)
    {
        Status = status;
    }

    public static DeepSeekException FromComplete(ClientResultException ex)
    {
        ArgumentNullException.ThrowIfNull(ex);

        var detail = Truncate(ex.GetRawResponse()?.Content?.ToString() ?? ex.Message);
        return new DeepSeekException(
            $"DeepSeek chat completion failed (HTTP {ex.Status}): {detail}",
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
