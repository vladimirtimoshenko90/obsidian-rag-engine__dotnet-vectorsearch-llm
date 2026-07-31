namespace ObsidianRagEngine.Llm.Pricing;

/// <summary>
/// Per-1M-token tariff. All amounts are in US dollars.
/// <paramref name="cachedInputPer1M"/> of -1 means "no separate cache rate"
/// (cached tokens billed at <paramref name="inputPer1M"/>).
/// Attribute args use <see cref="double"/> because <see cref="decimal"/> is not allowed on attributes.
/// </summary>
[AttributeUsage(AttributeTargets.Field)]
public sealed class LlmTariffAttribute(
    double inputPer1M,
    double outputPer1M,
    double cachedInputPer1M = -1) : Attribute
{
    public decimal InputPer1M { get; } = (decimal)inputPer1M;
    public decimal OutputPer1M { get; } = (decimal)outputPer1M;
    public decimal? CachedInputPer1M { get; } =
        cachedInputPer1M < 0 ? null : (decimal)cachedInputPer1M;
}
