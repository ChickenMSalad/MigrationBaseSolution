namespace MigrationBase.Core.Cloud.Azure.Infrastructure;

public sealed class AzureInfrastructureClientDescriptor
{
    public required string Name { get; init; }

    public AzureInfrastructureClientKind Kind { get; init; } = AzureInfrastructureClientKind.Unknown;

    public string? ConfigurationSection { get; init; }

    public string? EnvironmentName { get; init; }

    public string? Purpose { get; init; }

    public bool RequiresManagedIdentity { get; init; }

    public bool RequiresSecretMaterial { get; init; }

    public IList<string> RequiredSettings { get; } = new List<string>();

    public IList<string> Capabilities { get; } = new List<string>();
}
