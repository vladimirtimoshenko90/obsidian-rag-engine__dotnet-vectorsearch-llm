namespace ObsidianRagEngine.Contracts;

public interface ILlmProvider
{
    string ModelName { get; }

    Task<LlmCallResult> Complete(string prompt, CancellationToken ct, bool thinkingMode = false);
}
