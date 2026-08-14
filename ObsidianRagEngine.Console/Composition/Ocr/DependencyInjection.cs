using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ObsidianRagEngine.Contracts;
using ObsidianRagEngine.Ocr.Instruments.Tesseract;

namespace ObsidianRagEngine.Console.Composition.Ocr;

public static class DependencyInjection
{
    public static IServiceCollection AddOcr(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddHttpClient<IOcrProvider, TesseractOcrService>((_, client) =>
        {
            client.BaseAddress = new Uri(configuration["Tesseract:Url"]!);
        });

        return services;
    }
}
