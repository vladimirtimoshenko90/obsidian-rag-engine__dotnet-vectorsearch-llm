namespace ObsidianRagEngine.Contracts;

public interface ILlmJsonProvider
{
    string ModelName { get; }
    Task<LlmCallResult<T>> AskJson<T>(string question, CancellationToken ct, bool thinkingMode = false);
}
