using System;

namespace MigrationBase.Core.Cloud.Azure.FailureRuntime;

public sealed class AzureReplayAdmissionRequest
{
    public required AzureFailureSignal Signal { get; init; }

    public required AzureReplayEligibilityDecision Eligibility { get; init; }

    public string? RequestedBy { get; init; }

    public DateTimeOffset RequestedAtUtc { get; init; } = DateTimeOffset.UtcNow;

    public bool RequireApproval { get; init; } = true;

    public bool ApprovalGranted { get; init; }
}
