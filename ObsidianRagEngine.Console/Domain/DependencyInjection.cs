using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ObsidianRagEngine.Console.Domain.ObsidianVault;

namespace ObsidianRagEngine.Console.Domain;

public static class DependencyInjection
{
    public static IServiceCollection AddDomainLayer(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<ObsidianVaultSettings>(configuration.GetSection("ObsidianVault"));
        services.AddSingleton<IObsidianVaultReader, ObsidianVaultReader>();

        return services;
    }
}
