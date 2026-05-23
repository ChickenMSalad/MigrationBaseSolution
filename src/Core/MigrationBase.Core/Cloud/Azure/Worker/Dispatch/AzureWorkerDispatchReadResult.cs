using System.Collections.Generic;

namespace MigrationBase.Core.Cloud.Azure.Worker.Dispatch;

public sealed class AzureWorkerDispatchReadResult
{
    public IReadOnlyList<AzureWorkerDispatchEnvelope> Envelopes { get; init; } =
        new List<AzureWorkerDispatchEnvelope>();

    public bool HasMessages => Envelopes.Count > 0;

    public static AzureWorkerDispatchReadResult Empty { get; } = new();

    public static AzureWorkerDispatchReadResult FromEnvelopes(
        IReadOnlyList<AzureWorkerDispatchEnvelope> envelopes)
    {
        return new AzureWorkerDispatchReadResult
        {
            Envelopes = envelopes
        };
    }
}
