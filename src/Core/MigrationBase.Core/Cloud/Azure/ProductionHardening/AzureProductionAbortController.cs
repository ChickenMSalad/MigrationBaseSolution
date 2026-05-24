using System;

namespace MigrationBase.Core.Cloud.Azure.ProductionHardening;

public sealed class AzureProductionAbortController : IAzureProductionAbortController
{
    public AzureProductionAbortDecision Decide(AzureProductionAbortRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (string.IsNullOrWhiteSpace(request.ReleaseId))
        {
            return AzureProductionAbortDecision.Rejected("ReleaseId is required.");
        }

        if (string.IsNullOrWhiteSpace(request.Reason))
        {
            return AzureProductionAbortDecision.Rejected("Abort reason is required.");
        }

        if (!request.Confirmed)
        {
            return AzureProductionAbortDecision.Rejected(
                "Abort request must be explicitly confirmed.");
        }

        return AzureProductionAbortDecision.Approved(request.Reason);
    }
}
