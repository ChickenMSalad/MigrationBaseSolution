using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MigrationBase.Core.Cloud.Azure.FailureRuntime;

public sealed class InMemoryAzureFailureIncidentStore : IAzureFailureIncidentStore
{
    private readonly object gate = new();
    private readonly Dictionary<string, AzureFailureIncidentRecord> records =
        new(StringComparer.OrdinalIgnoreCase);

    public Task<AzureFailureIncidentRecordResult> RecordAsync(
        AzureFailureIncidentRecordRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.Signal);
        ArgumentNullException.ThrowIfNull(request.Classification);

        cancellationToken.ThrowIfCancellationRequested();

        var incidentId = Guid.NewGuid().ToString("n");

        var evidence = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["classification"] = request.Classification.Classification.ToString(),
            ["severity"] = request.Classification.Severity.ToString(),
            ["retryRecommended"] = request.Classification.RetryRecommended.ToString(),
            ["replayRecommended"] = request.Classification.ReplayRecommended.ToString()
        };

        if (!string.IsNullOrWhiteSpace(request.Signal.ErrorCode))
        {
            evidence["errorCode"] = request.Signal.ErrorCode;
        }

        var status = DetermineStatus(request);

        var record = new AzureFailureIncidentRecord
        {
            IncidentId = incidentId,
            Signal = request.Signal,
            Classification = request.Classification,
            RetryDecision = request.RetryDecision,
            ReplayEligibility = request.ReplayEligibility,
            ReplayAdmission = request.ReplayAdmission,
            Status = status,
            RecordedAtUtc = request.RequestedAtUtc,
            Evidence = evidence
        };

        lock (gate)
        {
            records[incidentId] = record;
        }

        return Task.FromResult(AzureFailureIncidentRecordResult.Success(record));
    }

    public Task<AzureFailureIncidentRecord?> GetAsync(
        string incidentId,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(incidentId))
        {
            return Task.FromResult<AzureFailureIncidentRecord?>(null);
        }

        lock (gate)
        {
            return Task.FromResult(
                records.TryGetValue(incidentId, out var record)
                    ? record
                    : null);
        }
    }

    private static AzureFailureIncidentStatus DetermineStatus(
        AzureFailureIncidentRecordRequest request)
    {
        if (request.RetryDecision is not null && request.RetryDecision.ShouldRetry)
        {
            return AzureFailureIncidentStatus.RetryScheduled;
        }

        if (request.ReplayAdmission is not null && request.ReplayAdmission.Admitted)
        {
            return AzureFailureIncidentStatus.ReplayRequested;
        }

        if (request.Classification.Classification == AzureFailureClassification.Poison)
        {
            return AzureFailureIncidentStatus.DeadLettered;
        }

        return AzureFailureIncidentStatus.Open;
    }
}
