namespace ObsidianRagEngine.Contracts;

public interface ILlmProvider
{
    string ModelName { get; }
    Task<string> Complete(string prompt, CancellationToken ct);
}
