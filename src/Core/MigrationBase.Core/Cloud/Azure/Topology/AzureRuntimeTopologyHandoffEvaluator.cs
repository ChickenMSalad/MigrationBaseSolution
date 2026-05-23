namespace MigrationBase.Core.Cloud.Azure.Topology;

/// <summary>
/// Small deterministic helper used by validation/reporting tools to decide whether
/// the P5.1 topology package is ready to be consumed by P5.2 worker stabilization.
/// </summary>
public static class AzureRuntimeTopologyHandoffEvaluator
{
    public static AzureRuntimeTopologyHandoffStatus Evaluate(AzureRuntimeTopologyHandoffManifest? manifest)
    {
        if (manifest is null)
        {
            return AzureRuntimeTopologyHandoffStatus.Unknown;
        }

        if (manifest.Items.Count == 0)
        {
            return AzureRuntimeTopologyHandoffStatus.NotStarted;
        }

        var requiredItems = manifest.Items.Where(item => item.RequiredForP52).ToArray();
        if (requiredItems.Length == 0)
        {
            return AzureRuntimeTopologyHandoffStatus.ReadyForWorkerStabilization;
        }

        if (requiredItems.Any(item => item.Status == AzureRuntimeTopologyHandoffStatus.Blocked))
        {
            return AzureRuntimeTopologyHandoffStatus.Blocked;
        }

        if (requiredItems.All(item => item.Status == AzureRuntimeTopologyHandoffStatus.ReadyForWorkerStabilization))
        {
            return AzureRuntimeTopologyHandoffStatus.ReadyForWorkerStabilization;
        }

        return AzureRuntimeTopologyHandoffStatus.InProgress;
    }
}
