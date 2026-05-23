using System;

namespace MigrationBase.Core.Cloud.Azure.FailureRuntime;

public sealed class AzureFailureIncidentRecordRequest
{
    public required AzureFailureSignal Signal { get; init; }

    public required AzureFailureClassificationResult Classification { get; init; }

    public AzureRetryDecision? RetryDecision { get; init; }

    public AzureReplayEligibilityDecision? ReplayEligibility { get; init; }

    public AzureReplayAdmissionDecision? ReplayAdmission { get; init; }

    public DateTimeOffset RequestedAtUtc { get; init; } = DateTimeOffset.UtcNow;
}
