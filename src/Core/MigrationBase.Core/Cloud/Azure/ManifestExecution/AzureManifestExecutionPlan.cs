using System;
using System.Collections.Generic;

namespace MigrationBase.Core.Cloud.Azure.ManifestExecution;

public sealed class AzureManifestExecutionPlan
{
    public required string PlanId { get; init; }

    public required AzureManifestExecutionScope Scope { get; init; }

    public DateTimeOffset CreatedAtUtc { get; init; } = DateTimeOffset.UtcNow;

    public IReadOnlyList<AzureManifestExecutionPlanStep> Steps { get; init; } =
        new List<AzureManifestExecutionPlanStep>();

    public IReadOnlyDictionary<string, string> Metadata { get; init; } =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
}
