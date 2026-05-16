namespace Migration.ControlPlane.Connectors;

/// <summary>
/// Normalized connector capability contract used by the cloud-facing Admin API.
/// This intentionally sits above concrete connector implementations so the UI,
/// validation layer, and future cloud worker can reason about connectors without
/// depending on host-specific registration details.
/// </summary>
public sealed record ConnectorCapabilityDescriptor(
    string Key,
    string DisplayName,
    string Role,
    string? Description,
    IReadOnlyList<string> Aliases,
    IReadOnlyList<string> SupportedOperations,
    IReadOnlyList<ConnectorConfigurationFieldDescriptor> ConfigurationFields,
    IReadOnlyList<ConnectorCredentialRequirementDescriptor> CredentialRequirements,
    bool SupportsManifestGeneration,
    bool SupportsValidation,
    bool SupportsDryRun);

public sealed record ConnectorConfigurationFieldDescriptor(
    string Name,
    string Label,
    string FieldType,
    bool Required,
    string? Description = null,
    string? DefaultValue = null,
    IReadOnlyList<string>? Options = null);

public sealed record ConnectorCredentialRequirementDescriptor(
    string Name,
    string Label,
    string SecretKind,
    bool Required,
    string? Description = null);

public static class ConnectorCapabilityRoles
{
    public const string Source = "source";
    public const string Target = "target";
    public const string ManifestProvider = "manifestProvider";
}
