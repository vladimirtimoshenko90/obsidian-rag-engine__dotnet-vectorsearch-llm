namespace ObsidianRagEngine.Llm;

public interface ILlmService
{
    Task<string> Generate(string prompt, CancellationToken ct);
}
