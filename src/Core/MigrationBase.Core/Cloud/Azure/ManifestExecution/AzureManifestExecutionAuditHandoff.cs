using System;
using System.Collections.Generic;

namespace MigrationBase.Core.Cloud.Azure.ManifestExecution;

public sealed class AzureManifestExecutionAuditHandoff
{
    public required string HandoffId { get; init; }

    public required AzureManifestExecutionCompletionRecord Completion { get; init; }

    public DateTimeOffset CreatedAtUtc { get; init; } = DateTimeOffset.UtcNow;

    public IReadOnlyList<string> RequiredEvidenceKeys { get; init; } =
        new[]
        {
            "planId",
            "mode",
            "status"
        };
}
