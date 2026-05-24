using System.Collections.Generic;

namespace MigrationBase.Core.Cloud.Azure.ConnectorExecution;

public sealed class AzureConnectorExecutionHandoffDescriptor
{
    public string Area { get; init; } = "P6.7 Connector Execution Runtime";

    public string NextArea { get; init; } = "P6.8 End-to-End Runtime Validation";

    public IReadOnlyList<string> CompletedCapabilities { get; init; } =
        new[]
        {
            "connector execution request/result boundary",
            "no-op connector executor",
            "manifest item handler adapter",
            "connector execution preflight validation",
            "connector failure signal mapping",
            "connector evidence and audit capture",
            "connector execution readiness evaluation"
        };

    public IReadOnlyList<string> HandoffExpectations { get; init; } =
        new[]
        {
            "end-to-end validation should exercise queue dispatch through manifest execution into connector execution",
            "real connector implementations should replace no-op executor behind IAzureConnectorExecutor",
            "connector failures should emit stable error codes for failure runtime classification",
            "connector evidence should be persisted to SQL before production use"
        };
}
