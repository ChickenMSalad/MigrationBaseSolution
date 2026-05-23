using System.Collections.Generic;

namespace MigrationBase.Core.Cloud.Azure.Worker.Dispatch;

public sealed class AzureWorkerDispatchClaimBatchResult
{
    public IReadOnlyList<AzureWorkerDispatchClaimedWorkItem> ClaimedItems { get; init; } =
        new List<AzureWorkerDispatchClaimedWorkItem>();

    public IReadOnlyList<string> RejectionReasons { get; init; } =
        new List<string>();

    public bool HasClaimedItems => ClaimedItems.Count > 0;

    public static AzureWorkerDispatchClaimBatchResult Empty { get; } = new();
}
