namespace ObsidianRagEngine.Contracts;

public interface ILlmService
{
    string ModelName { get; }
    Task<string> Generate(string prompt, CancellationToken ct);
}
