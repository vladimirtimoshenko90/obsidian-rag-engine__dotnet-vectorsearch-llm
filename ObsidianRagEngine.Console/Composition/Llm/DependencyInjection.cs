using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using ObsidianRagEngine.Contracts;
using ObsidianRagEngine.Llm.DeepSeekOllama;

namespace ObsidianRagEngine.Console.Composition.Llm;

public static class DependencyInjection
{
    public static IServiceCollection AddLlm(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<DeepSeekOllamaSettings>(configuration.GetSection("Ollama"));

        services.AddHttpClient<ILlmProvider, DeepSeekOllamaService>((sp, client) =>
        {
            var settings = sp.GetRequiredService<IOptions<DeepSeekOllamaSettings>>().Value;
            client.BaseAddress = new Uri(settings.Url);
        });

        return services;
    }
}
