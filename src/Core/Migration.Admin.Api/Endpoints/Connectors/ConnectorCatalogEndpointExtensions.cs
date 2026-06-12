using Migration.Orchestration.Abstractions;

namespace Migration.Admin.Api.Endpoints;

/// <summary>
/// Centralized Admin API connector-catalog endpoints.
/// Keep connector catalog routes in one place so Program.cs does not accumulate
/// duplicate route mappings as the cloud roadmap adds connector descriptors,
/// credential schemas, and dynamic UI metadata.
/// </summary>
public static class ConnectorCatalogEndpointExtensions
{
    public static RouteGroupBuilder MapConnectorCatalogEndpoints(this RouteGroupBuilder api)
    {
        ArgumentNullException.ThrowIfNull(api);

        api.MapGet("/connectors", (IConnectorCatalog catalog) => Results.Ok(new
            {
                sources = catalog.GetSources(),
                targets = catalog.GetTargets(),
                manifestProviders = catalog.GetManifestProviders()
            }))
            .WithName("GetConnectorCatalog")
            .WithTags("Connectors")
            .WithSummary("Gets all registered source, target, and manifest connector descriptors.");

        api.MapGet("/connectors/sources", (IConnectorCatalog catalog) => Results.Ok(catalog.GetSources()))
            .WithName("GetSourceConnectors")
            .WithTags("Connectors")
            .WithSummary("Gets source connector descriptors.");

        api.MapGet("/connectors/targets", (IConnectorCatalog catalog) => Results.Ok(catalog.GetTargets()))
            .WithName("GetTargetConnectors")
            .WithTags("Connectors")
            .WithSummary("Gets target connector descriptors.");

        api.MapGet("/connectors/manifests", (IConnectorCatalog catalog) => Results.Ok(catalog.GetManifestProviders()))
            .WithName("GetManifestProviders")
            .WithTags("Connectors")
            .WithSummary("Gets manifest provider descriptors.");

        api.MapGet("/manifest-providers", (IConnectorCatalog catalog) => Results.Ok(catalog.GetManifestProviders()))
            .WithName("GetManifestProvidersLegacy")
            .WithTags("Connectors")
            .WithSummary("Legacy alias for manifest provider descriptors.");

        return api;
    }
}


