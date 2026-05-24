namespace MigrationBase.Core.Cloud.Azure.EndToEndValidation;

public sealed class AzureEndToEndValidationIssue
{
    public required string Code { get; init; }

    public required string Message { get; init; }

    public string? Component { get; init; }

    public bool IsWarning { get; init; }
}
