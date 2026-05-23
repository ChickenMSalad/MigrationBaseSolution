namespace MigrationBase.Core.Cloud.Azure.Runtime.Composition;

public sealed record AzureRuntimeCompositionStep
{
    public required string Name { get; init; }

    public AzureRuntimeCompositionStepKind Kind { get; init; }

    public required string Purpose { get; init; }

    public bool Required { get; init; } = true;

    public IReadOnlyList<string> DependsOn { get; init; } = Array.Empty<string>();
}
