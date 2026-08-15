using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ObsidianRagEngine.Contracts;
using ObsidianRagEngine.Llm.OpenRouter;
using OpenAI;
using System.ClientModel;

namespace ObsidianRagEngine.Console.Composition.Llm;

public static class DependencyInjection
{
    public static IServiceCollection AddLlm(this IServiceCollection services, IConfiguration configuration)
    {
        var apiKey = configuration["Llm:OpenRouter:ApiKey"]
            ?? throw new InvalidOperationException("Llm:OpenRouter:ApiKey is required.");
        var endpoint = configuration["Llm:OpenRouter:Endpoint"]
            ?? throw new InvalidOperationException("Llm:OpenRouter:Endpoint is required.");

        services.AddSingleton<ILlmProvider>(_ =>
        {
            var client = new OpenAIClient(
                new ApiKeyCredential(apiKey),
                new OpenAIClientOptions
                {
                    Endpoint = new Uri(endpoint),
                    NetworkTimeout = TimeSpan.FromMinutes(10),
                });

            return new OpenRouterService(client, OpenRouterAiModel.DeepSeekV4Pro);
        });

        return services;
    }
}
