namespace MigrationBase.Core.Cloud.Azure.Worker.Dispatch;

public sealed class AzureWorkerDispatchClaimedWorkItem
{
    public required AzureWorkerDispatchEnvelope Envelope { get; init; }

    public required AzureWorkerDispatchClaim Claim { get; init; }
}
