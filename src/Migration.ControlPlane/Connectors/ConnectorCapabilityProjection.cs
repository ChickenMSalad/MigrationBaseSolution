using System.Reflection;

namespace Migration.ControlPlane.Connectors;

/// <summary>
/// Converts existing connector catalog descriptors into a stable capability
/// contract. The projection is deliberately conservative: it preserves existing
/// descriptor metadata when present and supplies cloud-roadmap defaults without
/// changing connector registrations or runtime behavior.
/// </summary>
public static class ConnectorCapabilityProjection
{
    public static ConnectorCapabilityDescriptor FromCatalogDescriptor(object descriptor, string role)
    {
        ArgumentNullException.ThrowIfNull(descriptor);

        var key = FirstNonEmpty(
            GetString(descriptor, "Type"),
            GetString(descriptor, "SourceType"),
            GetString(descriptor, "TargetType"),
            GetString(descriptor, "ManifestType"),
            GetString(descriptor, "Name"));

        key = ConnectorDescriptorAliases.Normalize(key);

        var displayName = FirstNonEmpty(
            GetString(descriptor, "DisplayName"),
            GetString(descriptor, "Name"),
            key);

        var description = FirstNonEmptyOrNull(GetString(descriptor, "Description"));

        return new ConnectorCapabilityDescriptor(
            Key: key,
            DisplayName: displayName,
            Role: role,
            Description: description,
            Aliases: ConnectorDescriptorAliases.GetAliases(key).ToArray(),
            SupportedOperations: GetSupportedOperations(role),
            ConfigurationFields: Array.Empty<ConnectorConfigurationFieldDescriptor>(),
            CredentialRequirements: Array.Empty<ConnectorCredentialRequirementDescriptor>(),
            SupportsManifestGeneration: role is ConnectorCapabilityRoles.Source or ConnectorCapabilityRoles.ManifestProvider,
            SupportsValidation: true,
            SupportsDryRun: true);
    }

    private static IReadOnlyList<string> GetSupportedOperations(string role) =>
        role switch
        {
            ConnectorCapabilityRoles.Source => new[] { "discover", "manifest", "validate", "read" },
            ConnectorCapabilityRoles.Target => new[] { "validate", "write", "dryRun" },
            ConnectorCapabilityRoles.ManifestProvider => new[] { "load", "validate", "schema" },
            _ => new[] { "validate" }
        };

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
