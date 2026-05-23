using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MigrationBase.Core.Cloud.Azure.ManifestExecution;

public sealed class InMemoryAzureManifestExecutionCheckpointStore :
    IAzureManifestExecutionCheckpointStore
{
    private readonly object gate = new();
    private readonly Dictionary<string, List<AzureManifestExecutionCheckpoint>> checkpoints =
        new(StringComparer.OrdinalIgnoreCase);

    public Task<AzureManifestExecutionCheckpointResult> RecordAsync(
        AzureManifestExecutionCheckpointRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(request.ExecutionId))
        {
            return Task.FromResult(
                AzureManifestExecutionCheckpointResult.Rejected(
                    "ExecutionId is required."));
        }

        if (string.IsNullOrWhiteSpace(request.StepId))
        {
            return Task.FromResult(
                AzureManifestExecutionCheckpointResult.Rejected(
                    "StepId is required."));
        }

        var checkpoint = new AzureManifestExecutionCheckpoint
        {
            CheckpointId = Guid.NewGuid().ToString("n"),
            StepId = request.StepId,
            RecordedAtUtc = request.RequestedAtUtc,
            Cursor = request.Cursor,
            ProcessedCount = request.ProcessedCount,
            Notes = request.Notes
        };

        lock (gate)
        {
            if (!checkpoints.TryGetValue(request.ExecutionId, out var list))
            {
                list = new List<AzureManifestExecutionCheckpoint>();
                checkpoints[request.ExecutionId] = list;
            }

            list.Add(checkpoint);
        }

        return Task.FromResult(
            AzureManifestExecutionCheckpointResult.Success(checkpoint));
    }

    public Task<AzureManifestExecutionCheckpoint?> GetLatestAsync(
        string executionId,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(executionId))
        {
            return Task.FromResult<AzureManifestExecutionCheckpoint?>(null);
        }

        lock (gate)
        {
            if (!checkpoints.TryGetValue(executionId, out var list) ||
                list.Count == 0)
            {
                return Task.FromResult<AzureManifestExecutionCheckpoint?>(null);
            }

            return Task.FromResult<AzureManifestExecutionCheckpoint?>(
                list
                    .OrderByDescending(checkpoint => checkpoint.RecordedAtUtc)
                    .FirstOrDefault());
        }
    }
}
