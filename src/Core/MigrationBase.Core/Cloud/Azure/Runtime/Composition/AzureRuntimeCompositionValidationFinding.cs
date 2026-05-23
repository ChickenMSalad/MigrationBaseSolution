namespace MigrationBase.Core.Cloud.Azure.Runtime.Composition;

public sealed class AzureRuntimeCompositionValidationFinding
{
    public required string Code { get; init; }

    public required string Message { get; init; }

    public AzureRuntimeCompositionValidationSeverity Severity { get; init; } = AzureRuntimeCompositionValidationSeverity.Warning;

    public string? Target { get; init; }

    public string? RecommendedAction { get; init; }
}
