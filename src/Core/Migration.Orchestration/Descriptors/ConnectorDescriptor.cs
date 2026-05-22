namespace Migration.Orchestration.Descriptors;

public sealed class ConnectorDescriptor
{
    public string Type { get; init; } = "";
    public string DisplayName { get; init; } = "";

    /// <summary>
    /// Existing catalog code uses Direction.
    /// </summary>
    public ConnectorDirections Direction { get; init; }

    /// <summary>
    /// Optional string kind retained for UI/cloud metadata.
    /// If not explicitly set, this can be inferred from Direction by callers.
    /// </summary>
    public string Kind { get; init; } = "";

    public string Description { get; init; } = "";

    public List<ConnectorCapabilities> Capabilities { get; init; } = new();

    public List<CredentialDescriptor> Credentials { get; init; } = new();

    public List<ConnectorOptionDescriptor> Options { get; init; } = new();

    public List<string> ManifestColumns { get; init; } = new();

    public List<string> MappingFields { get; init; } = new();

    public Dictionary<string, string> Metadata { get; init; } =
        new(StringComparer.OrdinalIgnoreCase);
}
