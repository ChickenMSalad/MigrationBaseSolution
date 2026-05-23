namespace MigrationBase.Core.Cloud.Azure.Infrastructure;

public sealed class AzureInfrastructureClientValidationIssue
{
    public required string Code { get; init; }

    public required string Message { get; init; }

    public string? ClientName { get; init; }

    public string? SettingName { get; init; }
}
