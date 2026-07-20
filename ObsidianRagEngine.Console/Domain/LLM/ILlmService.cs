namespace ObsidianRagEngine.Console.Domain.LLM;

public interface ILlmService
{
    Task<string> GenerateResponse(string prompt, CancellationToken ct = default);
}
