using System.Reflection;

namespace Migration.ControlPlane.Connectors;

/// <summary>
/// Converts existing connector catalog descriptors into a stable capability
/// contract. The projection preserves the existing catalog as source of truth
/// and enriches the output with cloud-facing configuration/credential metadata.
/// </summary>
public static class ConnectorCapabilityProjection
{
    public static ConnectorCapabilityDescriptor FromCatalogDescriptor(object descriptor, string role)
    {
        ArgumentNullException.ThrowIfNull(descriptor);

        var rawKey = FirstNonEmpty(
            GetString(descriptor, "Type"),
            GetString(descriptor, "SourceType"),
            GetString(descriptor, "TargetType"),
            GetString(descriptor, "ManifestType"),
            GetString(descriptor, "Name"));

        var normalizedKey = ConnectorDescriptorAliases.Normalize(rawKey);

        var displayName = FirstNonEmpty(
            GetString(descriptor, "DisplayName"),
            GetString(descriptor, "Name"),
            normalizedKey);

        var catalogDescription = FirstNonEmptyOrNull(GetString(descriptor, "Description"));
        var enrichment = ConnectorCapabilityRegistry.Get(role, normalizedKey);

        return new ConnectorCapabilityDescriptor(
            Key: normalizedKey,
            DisplayName: displayName,
            Role: role,
            Description: catalogDescription ?? enrichment.Description,
            Aliases: ConnectorDescriptorAliases.GetAliases(normalizedKey).ToArray(),
            SupportedOperations: MergeOperations(GetSupportedOperations(role), enrichment.SupportedOperations),
            ConfigurationFields: enrichment.ConfigurationFields,
            CredentialRequirements: enrichment.CredentialRequirements,
            SupportsManifestGeneration: enrichment.SupportsManifestGeneration || role is ConnectorCapabilityRoles.Source or ConnectorCapabilityRoles.ManifestProvider,
            SupportsValidation: enrichment.SupportsValidation,
            SupportsDryRun: enrichment.SupportsDryRun);
    }

    private static IReadOnlyList<string> GetSupportedOperations(string role) =>
        role switch
        {
            ConnectorCapabilityRoles.Source => new[] { "discover", "manifest", "validate", "read" },
            ConnectorCapabilityRoles.Target => new[] { "validate", "write", "dryRun" },
            ConnectorCapabilityRoles.ManifestProvider => new[] { "load", "validate", "schema" },
            _ => new[] { "validate" }
        };

    private static IReadOnlyList<string> MergeOperations(
        IReadOnlyList<string> defaults,
        IReadOnlyList<string> configured)
    {
        if (configured.Count == 0)
        {
            return defaults;
        }

        return defaults
            .Concat(configured)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static string FirstNonEmpty(params string?[] values)
    {
        foreach (var value in values)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value.Trim();
            }
        }

        return string.Empty;
    }

    private static string? FirstNonEmptyOrNull(params string?[] values)
    {
        var value = FirstNonEmpty(values);
        return string.IsNullOrWhiteSpace(value) ? null : value;
    }

    private static string? GetString(object instance, string propertyName)
    {
        var property = instance
            .GetType()
            .GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.IgnoreCase);

        var value = property?.GetValue(instance);
        return value?.ToString();
    }
}
