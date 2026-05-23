using System;

namespace MigrationBase.Core.Cloud.Azure.Worker.Dispatch;

public sealed class AzureWorkerDispatchDeferRequest
{
    public required AzureWorkerDispatchEnvelope Envelope { get; init; }

    public AzureWorkerDispatchClaim? Claim { get; init; }

    public required DateTimeOffset NotBeforeUtc { get; init; }

    public string? Reason { get; init; }
}
