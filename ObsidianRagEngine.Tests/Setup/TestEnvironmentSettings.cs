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
    }

    private static string Require(IConfiguration configuration, string key) =>
        configuration[key].Valuable()
            ? configuration[key]!
            : throw new InvalidOperationException($"Required setting '{key}' is missing or empty.");
}
