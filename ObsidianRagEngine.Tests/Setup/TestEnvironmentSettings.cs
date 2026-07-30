using Microsoft.Extensions.Configuration;
using ObsidianRagEngine.Console.Common.Extensions;

namespace ObsidianRagEngine.Tests.Setup;

/// <summary>
/// Loads test environment settings from appsettings (and environment overrides). Fully static.
/// </summary>
public static class TestEnvironmentSettings
{
    public static string TesseractUrl { get; }

    public static string OllamaUrl { get; }
    public static string OllamaLlmModel { get; }

    /// <summary>Cloud LLM credentials under the <c>Llm</c> config node (ApiKey + Endpoint only).</summary>
    public static OpenAiCompatibleSettings DeepSeek { get; }
    public static OpenAiCompatibleSettings Kimi { get; }
    public static OpenAiCompatibleSettings Alibaba { get; }

    static TestEnvironmentSettings()
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false)
            .AddEnvironmentVariables()
            .Build();

        TesseractUrl = Require(configuration, "Tesseract:Url");
        OllamaUrl = Require(configuration, "Ollama:Url");
        OllamaLlmModel = Require(configuration, "Ollama:LlmModel");

        DeepSeek = ReadOpenAi(configuration, "Llm:DeepSeek");
        Kimi = ReadOpenAi(configuration, "Llm:Kimi");
        Alibaba = ReadOpenAi(configuration, "Llm:Alibaba");
    }

    private static OpenAiCompatibleSettings ReadOpenAi(IConfiguration configuration, string section) =>
        new(
            Require(configuration, $"{section}:ApiKey"),
            new Uri(Require(configuration, $"{section}:Endpoint")));

    private static string Require(IConfiguration configuration, string key) =>
        configuration[key].Valuable()
            ? configuration[key]!
            : throw new InvalidOperationException($"Required setting '{key}' is missing or empty.");
}

/// <summary>ApiKey + Endpoint for an OpenAI-compatible vendor. Models come from each vendor's enum.</summary>
public sealed record OpenAiCompatibleSettings(string ApiKey, Uri Endpoint);
