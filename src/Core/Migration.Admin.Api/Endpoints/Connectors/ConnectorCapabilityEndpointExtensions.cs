using Migration.ControlPlane.Connectors;
using Migration.Orchestration.Abstractions;

namespace Migration.Admin.Api.Endpoints;

/// <summary>
/// Cloud-facing connector capability endpoints. These endpoints intentionally
/// project the existing IConnectorCatalog instead of creating a second catalog.
/// </summary>
public static class ConnectorCapabilityEndpointExtensions
{
    public static RouteGroupBuilder MapConnectorCapabilityEndpoints(this RouteGroupBuilder api)
    {
        ArgumentNullException.ThrowIfNull(api);

        api.MapGet("/connectors/capabilities", (IConnectorCatalog catalog) =>
            {
                var response = new
                {
                    sources = catalog
                        .GetSources()
                        .Select(descriptor => ConnectorCapabilityProjection.FromCatalogDescriptor(
                            descriptor,
                            ConnectorCapabilityRoles.Source))
                        .OrderBy(x => x.DisplayName)
                        .ToArray(),

                    targets = catalog
                        .GetTargets()
                        .Select(descriptor => ConnectorCapabilityProjection.FromCatalogDescriptor(
                            descriptor,
                            ConnectorCapabilityRoles.Target))
                        .OrderBy(x => x.DisplayName)
                        .ToArray(),

                    manifestProviders = catalog
                        .GetManifestProviders()
                        .Select(descriptor => ConnectorCapabilityProjection.FromCatalogDescriptor(
                            descriptor,
                            ConnectorCapabilityRoles.ManifestProvider))
                        .OrderBy(x => x.DisplayName)
                        .ToArray()
                };

                return Results.Ok(response);
            })
            .WithName("GetConnectorCapabilities")
            .WithTags("Connectors")
            .WithSummary("Gets normalized connector capabilities for cloud UI and validation workflows.");

        api.MapGet("/connectors/capabilities/{role}/{key}", (
                string role,
                string key,
                IConnectorCatalog catalog) =>
            {
                var normalizedRole = NormalizeRole(role);
                var normalizedKey = ConnectorDescriptorAliases.Normalize(key);

                IEnumerable<object>? descriptors = normalizedRole switch
                {
                    ConnectorCapabilityRoles.Source => catalog.GetSources().Cast<object>(),
                    ConnectorCapabilityRoles.Target => catalog.GetTargets().Cast<object>(),
                    ConnectorCapabilityRoles.ManifestProvider => catalog.GetManifestProviders().Cast<object>(),
                    _ => null
                };

                if (descriptors is null)
                {
                    return Results.BadRequest(new
                    {
                        error = $"Unsupported connector role '{role}'.",
                        allowedRoles = new[]
                        {
                            ConnectorCapabilityRoles.Source,
                            ConnectorCapabilityRoles.Target,
                            ConnectorCapabilityRoles.ManifestProvider
                        }
                    });
                }

                var capability = descriptors
                    .Select(descriptor => ConnectorCapabilityProjection.FromCatalogDescriptor(descriptor, normalizedRole))
                    .FirstOrDefault(descriptor => ConnectorDescriptorAliases.Matches(descriptor.Key, normalizedKey));

                return capability is null
                    ? Results.NotFound(new { error = $"Connector capability '{role}/{key}' was not found." })
                    : Results.Ok(capability);
            })
            .WithName("GetConnectorCapability")
            .WithTags("Connectors")
            .WithSummary("Gets one normalized connector capability by role and key.");

        return api;
    }

    private static string NormalizeRole(string role)
    {
        if (string.Equals(role, "source", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(role, "sources", StringComparison.OrdinalIgnoreCase))
        {
            return ConnectorCapabilityRoles.Source;
        }

        if (string.Equals(role, "target", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(role, "targets", StringComparison.OrdinalIgnoreCase))
        {
            return ConnectorCapabilityRoles.Target;
        }

        if (string.Equals(role, "manifest", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(role, "manifests", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(role, "manifestProvider", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(role, "manifestProviders", StringComparison.OrdinalIgnoreCase))
        {
            return ConnectorCapabilityRoles.ManifestProvider;
        }

        return role;
    }
}


