namespace Migration.Orchestration.Descriptors;

public sealed class CredentialDescriptor
{
    public string Name { get; init; } = "";
    public string DisplayName { get; init; } = "";
    public string ConfigurationKey { get; init; } = "";
    public bool Required { get; init; }
    public bool Secret { get; init; } = true;
}
