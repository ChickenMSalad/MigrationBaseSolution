namespace MigrationBase.Core.Cloud.Azure.FailureRuntime;

public sealed class AzureFailureRuntimeReadinessIssue
{
    public required string Code { get; init; }

    public required string Message { get; init; }

    public string? Component { get; init; }
}
