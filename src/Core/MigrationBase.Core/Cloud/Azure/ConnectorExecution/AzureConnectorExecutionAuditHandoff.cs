using System;
using System.Collections.Generic;

namespace MigrationBase.Core.Cloud.Azure.ConnectorExecution;

public sealed class AzureConnectorExecutionAuditHandoff
{
    public required string HandoffId { get; init; }

    public required AzureConnectorExecutionEvidenceRecord EvidenceRecord { get; init; }

    public DateTimeOffset CreatedAtUtc { get; init; } = DateTimeOffset.UtcNow;

    public IReadOnlyList<string> RequiredEvidenceKeys { get; init; } =
        new[]
        {
            "mode",
            "direction",
            "status"
        };
}
