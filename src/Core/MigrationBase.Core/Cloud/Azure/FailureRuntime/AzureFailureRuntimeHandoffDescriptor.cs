using System.Collections.Generic;

namespace MigrationBase.Core.Cloud.Azure.FailureRuntime;

public sealed class AzureFailureRuntimeHandoffDescriptor
{
    public string Area { get; init; } = "P6.6 Failure, Retry, and Replay Runtime";

    public string NextArea { get; init; } = "P6.7 Connector Execution Runtime";

    public IReadOnlyList<string> CompletedCapabilities { get; init; } =
        new[]
        {
            "failure signal classification",
            "retry policy and decision engine",
            "replay eligibility evaluation",
            "replay admission control",
            "failure incident recording",
            "failure runtime readiness evaluation"
        };

    public IReadOnlyList<string> HandoffExpectations { get; init; } =
        new[]
        {
            "connector execution should emit failure signals with stable error codes",
            "connector execution should honor retry and replay admission decisions",
            "incident history should be persisted to SQL before production use",
            "operator governance should remain authoritative for replay admission"
        };
}
