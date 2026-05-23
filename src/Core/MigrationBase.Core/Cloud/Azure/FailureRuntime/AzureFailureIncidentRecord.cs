using System;
using System.Collections.Generic;

namespace MigrationBase.Core.Cloud.Azure.FailureRuntime;

public sealed class AzureFailureIncidentRecord
{
    public required string IncidentId { get; init; }

    public required AzureFailureSignal Signal { get; init; }

    public required AzureFailureClassificationResult Classification { get; init; }

    public AzureRetryDecision? RetryDecision { get; init; }

    public AzureReplayEligibilityDecision? ReplayEligibility { get; init; }

    public AzureReplayAdmissionDecision? ReplayAdmission { get; init; }

    public AzureFailureIncidentStatus Status { get; init; } = AzureFailureIncidentStatus.Open;

    public DateTimeOffset RecordedAtUtc { get; init; } = DateTimeOffset.UtcNow;

    public IReadOnlyDictionary<string, string> Evidence { get; init; } =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
}
