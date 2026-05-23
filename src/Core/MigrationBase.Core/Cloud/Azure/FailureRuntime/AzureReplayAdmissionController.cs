using System;

namespace MigrationBase.Core.Cloud.Azure.FailureRuntime;

public sealed class AzureReplayAdmissionController : IAzureReplayAdmissionController
{
    public AzureReplayAdmissionDecision Decide(AzureReplayAdmissionRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.Signal);
        ArgumentNullException.ThrowIfNull(request.Eligibility);

        if (!request.Eligibility.Eligible)
        {
            return AzureReplayAdmissionDecision.Reject(
                request.Eligibility.Reason ?? "Replay is not eligible.");
        }

        if (request.RequireApproval && !request.ApprovalGranted)
        {
            return AzureReplayAdmissionDecision.Reject(
                "Replay requires approval before admission.");
        }

        return AzureReplayAdmissionDecision.Admit(
            request.Eligibility.Reason ?? "Replay admitted.");
    }
}
