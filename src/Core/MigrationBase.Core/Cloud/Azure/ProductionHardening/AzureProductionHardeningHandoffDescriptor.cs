using System.Collections.Generic;

namespace MigrationBase.Core.Cloud.Azure.ProductionHardening;

public sealed class AzureProductionHardeningHandoffDescriptor
{
    public string Area { get; init; } = "P6.9 Production Hardening";

    public string NextArea { get; init; } = "P6 Closeout / P7 Planning";

    public IReadOnlyList<string> CompletedCapabilities { get; init; } =
        new[]
        {
            "production release gate evaluation",
            "production rollback and abort decisioning",
            "production readiness checklist",
            "operator signoff record",
            "production deployment decision evidence",
            "production hardening readiness evaluation"
        };

    public IReadOnlyList<string> HandoffExpectations { get; init; } =
        new[]
        {
            "P6 closeout should validate all runtime slices compile together",
            "P7 planning should replace in-memory stores with SQL-backed implementations where required",
            "release gates should consume real end-to-end evidence before production use",
            "operator signoff and override records should be persisted before go-live"
        };
}
