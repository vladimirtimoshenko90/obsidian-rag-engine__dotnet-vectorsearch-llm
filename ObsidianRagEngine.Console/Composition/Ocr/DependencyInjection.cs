using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ObsidianRagEngine.Contracts;
using ObsidianRagEngine.Ocr.Domains.Messenger.SplitMerge;
using ObsidianRagEngine.Ocr.Instruments.Tesseract;

namespace ObsidianRagEngine.Console.Composition.Ocr;

public static class DependencyInjection
{
    public static IServiceCollection AddOcr(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddHttpClient<TesseractOcrService>((_, client) =>
        {
            client.BaseAddress = new Uri(configuration["Tesseract:Url"]!);
            client.Timeout = TimeSpan.FromMinutes(2);
        });

        services.AddTransient<IOcrProvider>(sp =>
            new MessengerSplitMergeOcrService(
                sp.GetRequiredService<TesseractOcrService>(),
                sp.GetRequiredService<ILlmProvider>()));

        return services;
    }
}
