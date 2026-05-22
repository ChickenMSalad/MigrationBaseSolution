using Microsoft.Extensions.DependencyInjection;

namespace Migration.ControlPlane.Storage;

public static class ArtifactStorageRegistrationExtensions
{
    public static IServiceCollection AddArtifactStorage(
        this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddSingleton<IArtifactStorageService, ArtifactStorageService>();

        return services;
    }
}
