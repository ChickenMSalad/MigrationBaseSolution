using System.Collections.Generic;

namespace MigrationBase.Core.Cloud.Azure.ManifestExecution;

public sealed class AzureManifestExecutionHandoffDescriptor
{
    public string Area { get; init; } = "P6.5 Manifest Execution";

    public string NextArea { get; init; } = "P6.6 Failure, Retry, and Replay Runtime";

    public IReadOnlyList<string> CompletedCapabilities { get; init; } =
        new[]
        {
            "manifest execution plan model",
            "manifest execution context and state policy",
            "manifest batch/item model",
            "batch execution runner",
            "checkpoint recording boundary",
            "completion and audit handoff boundary",
            "manifest execution readiness evaluation"
        };

    public IReadOnlyList<string> HandoffExpectations { get; init; } =
        new[]
        {
            "failure runtime should consume item, batch, checkpoint, and completion outcomes",
            "retry runtime should preserve execution context and checkpoint cursor semantics",
            "replay runtime should explicitly distinguish dry-run, validate-only, and execute modes",
            "SQL-backed implementations should replace in-memory stores before production use"
        };
}
