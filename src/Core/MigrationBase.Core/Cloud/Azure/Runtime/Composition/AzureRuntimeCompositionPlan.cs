namespace MigrationBase.Core.Cloud.Azure.Runtime.Composition;

public sealed record AzureRuntimeCompositionPlan
{
    public required string Name { get; init; }

    public required string EnvironmentName { get; init; }

    public required string HostRole { get; init; }

    public IReadOnlyList<AzureRuntimeCompositionStep> Steps { get; init; } = Array.Empty<AzureRuntimeCompositionStep>();

    public IReadOnlyList<string> RequiredConfigurationSections { get; init; } = Array.Empty<string>();

    public IReadOnlyList<string> RequiredOperationalStores { get; init; } = Array.Empty<string>();
}
