using System;
using System.Collections.Generic;

namespace MigrationBase.Core.Cloud.Azure.ExecutionValidation;

/// <summary>
/// Captures the outcome of a real migration validation scenario without coupling the contract to a specific persistence implementation.
/// </summary>
public sealed class AzureRealMigrationValidationResult
{
    public string ScenarioName { get; init; } = string.Empty;
    public string RunId { get; init; } = string.Empty;
    public DateTimeOffset EvaluatedAtUtc { get; init; } = DateTimeOffset.UtcNow;
    public bool Passed { get; init; }
    public int ManifestRowsObserved { get; init; }
    public int ItemsSucceeded { get; init; }
    public int ItemsFailed { get; init; }
    public int ItemsSkipped { get; init; }
    public IReadOnlyCollection<string> FailedCheckpoints { get; init; } = Array.Empty<string>();
    public IReadOnlyCollection<string> Warnings { get; init; } = Array.Empty<string>();

    public bool HasFailures => ItemsFailed > 0 || FailedCheckpoints.Count > 0;
}
