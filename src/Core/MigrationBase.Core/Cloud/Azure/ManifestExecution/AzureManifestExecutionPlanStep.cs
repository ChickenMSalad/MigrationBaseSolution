namespace MigrationBase.Core.Cloud.Azure.ManifestExecution;

public sealed class AzureManifestExecutionPlanStep
{
    public required string StepId { get; init; }

    public required string Name { get; init; }

    public int Order { get; init; }

    public bool Required { get; init; } = true;

    public string? Description { get; init; }
}
