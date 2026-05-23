using System.Collections.Generic;

namespace MigrationBase.Core.Cloud.Azure.ManifestExecution;

public sealed class AzureManifestExecutionItem
{
    public required string ItemId { get; init; }

    public required string ManifestId { get; init; }

    public string? SourceIdentifier { get; init; }

    public string? TargetIdentifier { get; init; }

    public IReadOnlyDictionary<string, string> Properties { get; init; } =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
}
