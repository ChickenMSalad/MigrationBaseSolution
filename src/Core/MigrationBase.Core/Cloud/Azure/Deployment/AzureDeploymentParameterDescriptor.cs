namespace MigrationBase.Core.Cloud.Azure.Deployment;

/// <summary>
/// Describes a named Azure deployment parameter without binding the runtime to a specific IaC engine.
/// </summary>
public sealed record AzureDeploymentParameterDescriptor
{
    public required string Name { get; init; }

    public string? Description { get; init; }

    public string? Category { get; init; }

    public string? ValueType { get; init; }

    public AzureDeploymentParameterRequirement Requirement { get; init; } = AzureDeploymentParameterRequirement.Optional;

    public bool IsSecret { get; init; }

    public bool AllowEnvironmentOverride { get; init; } = true;

    public IReadOnlyCollection<string> AppliesToHostRoles { get; init; } = Array.Empty<string>();

    public IReadOnlyCollection<string> AppliesToEnvironments { get; init; } = Array.Empty<string>();

    public string? DefaultValueHint { get; init; }
}
