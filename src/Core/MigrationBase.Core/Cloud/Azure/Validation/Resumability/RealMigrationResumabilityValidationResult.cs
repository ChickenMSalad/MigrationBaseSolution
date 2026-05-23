using System;
using System.Collections.Generic;

namespace MigrationBase.Core.Cloud.Azure.Validation.Resumability;

public sealed record RealMigrationResumabilityValidationResult
{
    public string MigrationRunId { get; init; } = string.Empty;
    public bool Passed { get; init; }
    public DateTimeOffset EvaluatedAtUtc { get; init; } = DateTimeOffset.UtcNow;
    public IReadOnlyList<string> Findings { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> BlockingIssues { get; init; } = Array.Empty<string>();
}
