namespace ObsidianRagEngine.Llm;

public interface ILlmService
{
    Task<string> GenerateResponse(string prompt, CancellationToken ct = default);
}
