using System;

namespace MigrationBase.Core.Cloud.Azure.ManifestExecution;

public sealed class AzureManifestExecutionItemResult
{
    public required string ItemId { get; init; }

    public AzureManifestExecutionItemResultStatus Status { get; init; } =
        AzureManifestExecutionItemResultStatus.Succeeded;

    public string? Message { get; init; }

    public string? ErrorCode { get; init; }

    public DateTimeOffset CompletedAtUtc { get; init; } = DateTimeOffset.UtcNow;
}
