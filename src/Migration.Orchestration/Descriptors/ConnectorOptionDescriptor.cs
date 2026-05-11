namespace Migration.Orchestration.Descriptors;

public sealed class ConnectorOptionDescriptor
{
    public string Name { get; init; } = "";

    public string DisplayName { get; init; } = "";

    public bool Required { get; init; }

    public string? DefaultValue { get; init; }

    public string? Description { get; init; }

    public List<string> AllowedValues { get; init; } = new();
}
