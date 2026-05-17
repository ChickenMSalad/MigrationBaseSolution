using Microsoft.Extensions.DependencyInjection;

namespace Migration.ControlPlane.Storage;

public static class ArtifactManifestIndexRegistrationExtensions
{
    public static IServiceCollection AddArtifactManifestIndex(
        this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddSingleton<IArtifactManifestIndexService, ArtifactManifestIndexService>();

        return services;
    }
}
