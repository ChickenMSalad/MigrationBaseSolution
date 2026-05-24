namespace MigrationBase.Core.Cloud.Azure.EndToEndValidation;

public sealed class AzureEndToEndValidationReadinessIssue
{
    public required string Code { get; init; }

    public required string Message { get; init; }

    public string? Component { get; init; }
}
