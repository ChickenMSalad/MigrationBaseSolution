using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MigrationBase.Core.Cloud.Azure.ConnectorExecution;

public sealed class InMemoryAzureConnectorExecutionEvidenceSink :
    IAzureConnectorExecutionEvidenceSink
{
    private readonly List<AzureConnectorExecutionEvidenceRecord> records = new();

    public IReadOnlyList<AzureConnectorExecutionEvidenceRecord> Records => records;

    public Task<AzureConnectorExecutionEvidenceResult> RecordAsync(
        AzureConnectorExecutionEvidenceRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.Request);
        ArgumentNullException.ThrowIfNull(request.Result);

        cancellationToken.ThrowIfCancellationRequested();

        if (!StringComparer.OrdinalIgnoreCase.Equals(
                request.Request.ItemId,
                request.Result.ItemId))
        {
            return Task.FromResult(
                AzureConnectorExecutionEvidenceResult.Rejected(
                    "Connector execution request and result refer to different item ids."));
        }

        var evidence = new Dictionary<string, string>(
            request.Result.Evidence,
            StringComparer.OrdinalIgnoreCase)
        {
            ["mode"] = request.Request.Mode.ToString(),
            ["direction"] = request.Request.Direction.ToString(),
            ["status"] = request.Result.Status.ToString()
        };

        var record = new AzureConnectorExecutionEvidenceRecord
        {
            EvidenceId = Guid.NewGuid().ToString("n"),
            ExecutionId = request.Request.ExecutionId,
            RunId = request.Request.RunId,
            ManifestId = request.Request.ManifestId,
            ItemId = request.Request.ItemId,
            Status = request.Result.Status,
            SourceIdentifier = request.Result.SourceIdentifier,
            TargetIdentifier = request.Result.TargetIdentifier,
            ErrorCode = request.Result.ErrorCode,
            Message = request.Result.Message,
            RecordedAtUtc = request.RequestedAtUtc,
            Evidence = evidence
        };

        records.Add(record);

        return Task.FromResult(
            AzureConnectorExecutionEvidenceResult.Success(record));
    }
}
