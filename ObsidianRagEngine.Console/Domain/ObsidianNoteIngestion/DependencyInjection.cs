using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using ObsidianRagEngine.Console.Domain.ObsidianNoteIngestion.Sanitization;
using ObsidianRagEngine.Console.Domain.ObsidianNoteIngestion.Vectorization;

namespace ObsidianRagEngine.Console.Domain.ObsidianNoteIngestion;

public static class DependencyInjection
{
    public static IServiceCollection AddObsidianNoteIngestion(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<OllamaEmbeddingSettings>(configuration.GetSection("Ollama"));
        services.AddHttpClient<IEmbeddingService, OllamaEmbeddingService>((sp, client) =>
        {
            var settings = sp.GetRequiredService<IOptions<OllamaEmbeddingSettings>>().Value;
            client.BaseAddress = new Uri(settings.Url);
        });

        services.AddSingleton<ITextChunkingService, TextChunkingService>();
        services.AddScoped<IObsidianNoteVectorizationService, ObsidianNoteVectorizationService>();
        services.AddScoped<IObsidianNoteSanitizationService, ObsidianNoteSanitizationService>();

        services.AddScoped<IObsidianNoteIngestionService, ObsidianNoteIngestionService>();

        return services;
    }
}
