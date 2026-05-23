using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MigrationBase.Core.Cloud.Azure.ManifestExecution;

public sealed class InMemoryAzureManifestExecutionCompletionSink :
    IAzureManifestExecutionCompletionSink
{
    private readonly List<AzureManifestExecutionCompletionRecord> records = new();

    public IReadOnlyList<AzureManifestExecutionCompletionRecord> Records => records;

    public Task<AzureManifestExecutionCompletionResult> CompleteAsync(
        AzureManifestExecutionCompletionRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.Context);
        ArgumentNullException.ThrowIfNull(request.Context.Plan);

        cancellationToken.ThrowIfCancellationRequested();

        var scope = request.Context.Plan.Scope;

        var evidence = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["planId"] = request.Context.Plan.PlanId,
            ["mode"] = scope.Mode.ToString(),
            ["status"] = request.Status.ToString()
        };

        if (!string.IsNullOrWhiteSpace(request.Reason))
        {
            evidence["reason"] = request.Reason;
        }

        if (!string.IsNullOrWhiteSpace(request.ErrorCode))
        {
            evidence["errorCode"] = request.ErrorCode;
        }

        var record = new AzureManifestExecutionCompletionRecord
        {
            CompletionId = Guid.NewGuid().ToString("n"),
            ExecutionId = request.Context.ExecutionId,
            RunId = scope.RunId,
            ManifestId = scope.ManifestId,
            Status = request.Status,
            CompletedAtUtc = request.CompletedAtUtc,
            Reason = request.Reason,
            ErrorCode = request.ErrorCode,
            FinalCursor = request.FinalCheckpoint?.Cursor,
            ProcessedCount = request.FinalCheckpoint?.ProcessedCount,
            Evidence = evidence
        };

        records.Add(record);

        return Task.FromResult(
            AzureManifestExecutionCompletionResult.Success(record));
    }
}
