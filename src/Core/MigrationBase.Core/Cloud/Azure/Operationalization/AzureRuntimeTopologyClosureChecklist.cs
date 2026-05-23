namespace MigrationBase.Core.Cloud.Azure.Operationalization;

/// <summary>
/// Provides the default P5.1 closeout gates used before starting worker stabilization.
/// </summary>
public static class AzureRuntimeTopologyClosureChecklist
{
    public static IReadOnlyList<AzureRuntimeTopologyClosureGate> CreateDefault()
    {
        return new[]
        {
            new AzureRuntimeTopologyClosureGate
            {
                GateId = "P5.1-CLOUD-TOPOLOGY",
                Name = "Cloud topology model defined",
                Area = "Azure Runtime Topology",
                Description = "Runtime, storage, queue, SQL, telemetry, and host-role boundaries have shared descriptors.",
                Status = AzureRuntimeTopologyReadinessStatus.NotStarted,
                RequiredForP52 = true,
                EvidenceKeys = new[] { "topology", "deployment-targets", "host-roles" }
            },
            new AzureRuntimeTopologyClosureGate
            {
                GateId = "P5.1-CONFIG-BOUNDARIES",
                Name = "Configuration and secret boundaries defined",
                Area = "Configuration",
                Description = "App setting, identity, tenant, and secret boundary contracts are available for host composition.",
                Status = AzureRuntimeTopologyReadinessStatus.NotStarted,
                RequiredForP52 = true,
                EvidenceKeys = new[] { "app-settings", "identity", "tenant-boundaries" }
            },
            new AzureRuntimeTopologyClosureGate
            {
                GateId = "P5.1-READINESS-EVIDENCE",
                Name = "Readiness evidence contracts defined",
                Area = "Operational Readiness",
                Description = "Deployment readiness, promotion, drift, capacity, and isolation evidence contracts are available.",
                Status = AzureRuntimeTopologyReadinessStatus.NotStarted,
                RequiredForP52 = true,
                EvidenceKeys = new[] { "readiness", "promotion", "drift", "capacity", "isolation" }
            }
        };
    }
}
