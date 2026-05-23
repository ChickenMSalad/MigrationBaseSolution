namespace MigrationBase.Core.Cloud.Azure.Capacity;

/// <summary>
/// Names a capacity expectation that should be satisfied before a migration environment is promoted or used for real execution.
/// </summary>
public sealed class AzureRuntimeCapacityRequirement
{
    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string Severity { get; set; } = "Warning";

    public string AppliesToRole { get; set; } = string.Empty;

    public string ExpectedValue { get; set; } = string.Empty;

    public string EvidenceKey { get; set; } = string.Empty;
}
