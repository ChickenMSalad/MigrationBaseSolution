namespace MigrationBase.Core.Cloud.Azure.ProductionHardening;

public sealed class AzureProductionReleaseGateIssue
{
    public required string Code { get; init; }

    public required string Message { get; init; }

    public string? Component { get; init; }

    public bool IsBlocking { get; init; } = true;
}
