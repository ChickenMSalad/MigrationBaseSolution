using System;

namespace MigrationBase.Core.Cloud.Azure.ExecutionValidation;

/// <summary>
/// Represents a named checkpoint that must be satisfied during real migration execution validation.
/// </summary>
public sealed class AzureRealMigrationValidationCheckpoint
{
    public string CheckpointName { get; init; } = string.Empty;
    public string Category { get; init; } = string.Empty;
    public string Requirement { get; init; } = string.Empty;
    public bool IsRequired { get; init; } = true;
    public int SortOrder { get; init; }
    public TimeSpan? ExpectedCompletionWindow { get; init; }

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(CheckpointName) &&
        !string.IsNullOrWhiteSpace(Category) &&
        !string.IsNullOrWhiteSpace(Requirement);
}
