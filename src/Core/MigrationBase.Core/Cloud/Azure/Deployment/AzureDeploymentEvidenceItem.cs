namespace MigrationBase.Core.Cloud.Azure.Deployment;

/// <summary>
/// Describes one piece of evidence collected during Azure deployment readiness validation.
/// Evidence is intentionally SDK-free so it can be produced by scripts, CI jobs, operators, or later Azure integrations.
/// </summary>
public sealed record AzureDeploymentEvidenceItem
{
    public string Key { get; init; } = string.Empty;

    public string Category { get; init; } = string.Empty;

    public string DisplayName { get; init; } = string.Empty;

    public string Source { get; init; } = string.Empty;

    public string Value { get; init; } = string.Empty;

    public string? ExpectedValue { get; init; }

    public bool IsRequired { get; init; } = true;

    public bool IsSatisfied { get; init; }

    public string? Notes { get; init; }
}
