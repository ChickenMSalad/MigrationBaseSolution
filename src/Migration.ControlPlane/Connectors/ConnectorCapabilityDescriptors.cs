namespace Migration.ControlPlane.Connectors;

/// <summary>
/// Normalized connector capability contract used by the cloud-facing Admin API.
/// This contract is intentionally independent from concrete connector implementations
/// so the frontend, validation layer, and worker orchestration can reason about
/// source/target requirements consistently.
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

public static class ConnectorFieldTypes
{
    public const string Text = "text";
    public const string Password = "password";
    public const string Url = "url";
    public const string Path = "path";
    public const string Boolean = "boolean";
    public const string Number = "number";
    public const string Select = "select";
    public const string MultiText = "multiText";
    public const string Json = "json";
}

public static class ConnectorSecretKinds
{
    public const string Username = "username";
    public const string Password = "password";
    public const string BearerToken = "bearerToken";
    public const string ApiKey = "apiKey";
    public const string ApiSecret = "apiSecret";
    public const string OAuthClientId = "oauthClientId";
    public const string OAuthClientSecret = "oauthClientSecret";
    public const string ConnectionString = "connectionString";
    public const string AccessKeyId = "accessKeyId";
    public const string SecretAccessKey = "secretAccessKey";
}
