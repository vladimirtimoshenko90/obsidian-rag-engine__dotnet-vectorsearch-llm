using System.ClientModel;

namespace ObsidianRagEngine.Llm.Exceptions;

public sealed class LlmException : Exception
{
    private const int MaxErrorBodyChars = 512;

    public int? Status { get; }

    public LlmException(string message, int? status = null, Exception? innerException = null)
        : base(message, innerException)
    {
        Status = status;
    }

    public static LlmException FromComplete(string provider, ClientResultException ex)
    {
        ArgumentNullException.ThrowIfNull(provider);
        ArgumentNullException.ThrowIfNull(ex);

        var detail = Truncate(ex.GetRawResponse()?.Content?.ToString() ?? ex.Message);
        return new LlmException(
            $"{provider} chat completion failed (HTTP {ex.Status}): {detail}",
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
