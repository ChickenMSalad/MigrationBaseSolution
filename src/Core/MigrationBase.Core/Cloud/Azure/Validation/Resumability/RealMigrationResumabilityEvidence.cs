using System;
using System.Collections.Generic;

namespace MigrationBase.Core.Cloud.Azure.Validation.Resumability;

public sealed record RealMigrationResumabilityEvidence
{
    public string EvidenceId { get; init; } = Guid.NewGuid().ToString("N");
    public string MigrationRunId { get; init; } = string.Empty;
    public string WorkItemId { get; init; } = string.Empty;
    public DateTimeOffset CapturedAtUtc { get; init; } = DateTimeOffset.UtcNow;
    public long LastDurableSourceOrdinal { get; init; }
    public long LastDurableTargetOrdinal { get; init; }
    public string ResumeToken { get; init; } = string.Empty;
    public IReadOnlyDictionary<string, string> Attributes { get; init; } = new Dictionary<string, string>();
}
