namespace MigrationBase.Core.Cloud.Azure.Workers.Stabilization;

/// <summary>
/// A single worker-stabilization readiness item used to decide whether P5.2 is complete enough
/// to begin deployment automation and real execution validation work.
/// </summary>
public sealed class AzureWorkerStabilizationChecklistItem
{
    public AzureWorkerStabilizationChecklistItem(
        string key,
        string name,
        AzureWorkerStabilizationReadinessStatus status,
        string owner,
        string evidenceReference,
        string notes)
    {
        Key = key ?? string.Empty;
        Name = name ?? string.Empty;
        Status = status;
        Owner = owner ?? string.Empty;
        EvidenceReference = evidenceReference ?? string.Empty;
        Notes = notes ?? string.Empty;
    }

    public string Key { get; }

    public string Name { get; }

    public AzureWorkerStabilizationReadinessStatus Status { get; }

    public string Owner { get; }

    public string EvidenceReference { get; }

    public string Notes { get; }

    public bool IsReady => Status == AzureWorkerStabilizationReadinessStatus.Validated;
}
