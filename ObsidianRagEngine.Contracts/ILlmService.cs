namespace ObsidianRagEngine.Contracts;

public interface ILlmService
{
    Task<string> Generate(string prompt, CancellationToken ct);
}
