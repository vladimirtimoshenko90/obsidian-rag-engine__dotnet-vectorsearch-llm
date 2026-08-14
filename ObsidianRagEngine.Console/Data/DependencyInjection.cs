using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ObsidianRagEngine.Console.Data.ObsidianNoteChunks.Repositories;
using ObsidianRagEngine.Console.Data.ObsidianNotes;
using ObsidianRagEngine.Console.Data.ObsidianNotes.Repositories;
using Qdrant.Client;

namespace ObsidianRagEngine.Console.Data;

public static class DependencyInjection
{
    public static IServiceCollection AddDataLayer(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<ObsidianNotesDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("ObsidianNotes")));

        services.AddSingleton(_ =>
            new QdrantClient(new Uri(configuration.GetConnectionString("ObsidianNoteChunks")!)));

        services.AddScoped<IObsidianNoteRepository, ObsidianNoteRepository>();
        services.AddScoped<IObsidianImageRepository, ObsidianImageRepository>();
        services.AddScoped<IObsidianNoteChunkRepository, ObsidianNoteChunkRepository>();

        return services;
    }
}
