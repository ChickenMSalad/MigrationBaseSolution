namespace Migration.Orchestration.Descriptors;

public sealed class ManifestProviderDescriptor
{
    public string Type { get; init; } = "";

    public string DisplayName { get; init; } = "";

    public string Description { get; init; } = "";

    public List<string> SupportedExtensions { get; init; } = new();

    /// <summary>
    /// ConventionConnectorCatalog builds this as a list of ConnectorOptionDescriptor.
    /// </summary>
    public List<ConnectorOptionDescriptor> Options { get; init; } = new();

    public Dictionary<string, string> Metadata { get; init; } =
        new(StringComparer.OrdinalIgnoreCase);
}
