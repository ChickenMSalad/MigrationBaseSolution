using System.Collections.Generic;

namespace MigrationBase.Core.Cloud.Azure.Worker.Dispatch;

public sealed class AzureWorkerDispatchHandoffDescriptor
{
    public string Area { get; init; } = "P6.4 Queue Dispatcher Mechanics";

    public string NextArea { get; init; } = "P6.5 Manifest Execution";

    public IReadOnlyList<string> CompletedCapabilities { get; init; } =
        new[]
        {
            "dispatch envelope and claim model",
            "queue reader/writer boundary",
            "read-and-claim coordinator",
            "completion acknowledgement boundary",
            "dead-letter and deferral boundary",
            "dispatcher readiness evaluation"
        };

    public IReadOnlyList<string> HandoffExpectations { get; init; } =
        new[]
        {
            "manifest execution should consume claimed dispatch envelopes",
            "manifest execution should complete, defer, or dead-letter dispatches explicitly",
            "SQL-backed implementations should replace in-memory stores before production use",
            "host wiring should remain opt-in until concrete worker composition is selected"
        };
}
