using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MigrationBase.Core.Cloud.Azure.Worker.Dispatch;

public sealed class AzureWorkerDispatcherReadinessEvaluator : IAzureWorkerDispatcherReadinessEvaluator
{
    private readonly IAzureWorkerDispatchQueueReader? queueReader;
    private readonly IAzureWorkerDispatchQueueWriter? queueWriter;
    private readonly IAzureWorkerDispatchClaimStore? claimStore;
    private readonly IAzureWorkerDispatchCompletionSink? completionSink;
    private readonly IAzureWorkerDispatchDeadLetterSink? deadLetterSink;

    public AzureWorkerDispatcherReadinessEvaluator(
        IAzureWorkerDispatchQueueReader? queueReader = null,
        IAzureWorkerDispatchQueueWriter? queueWriter = null,
        IAzureWorkerDispatchClaimStore? claimStore = null,
        IAzureWorkerDispatchCompletionSink? completionSink = null,
        IAzureWorkerDispatchDeadLetterSink? deadLetterSink = null)
    {
        this.queueReader = queueReader;
        this.queueWriter = queueWriter;
        this.claimStore = claimStore;
        this.completionSink = completionSink;
        this.deadLetterSink = deadLetterSink;
    }

    public Task<AzureWorkerDispatcherReadinessReport> EvaluateAsync(
        AzureWorkerDispatcherReadinessRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();

        var issues = new List<AzureWorkerDispatcherReadinessIssue>();

        AddMissingIssueIfRequired(
            issues,
            request.RequireQueueReader,
            queueReader,
            "queue-reader",
            "Worker dispatch queue reader is not registered.");

        AddMissingIssueIfRequired(
            issues,
            request.RequireQueueWriter,
            queueWriter,
            "queue-writer",
            "Worker dispatch queue writer is not registered.");

        AddMissingIssueIfRequired(
            issues,
            request.RequireClaimStore,
            claimStore,
            "claim-store",
            "Worker dispatch claim store is not registered.");

        AddMissingIssueIfRequired(
            issues,
            request.RequireCompletionSink,
            completionSink,
            "completion-sink",
            "Worker dispatch completion sink is not registered.");

        AddMissingIssueIfRequired(
            issues,
            request.RequireDeadLetterSink,
            deadLetterSink,
            "dead-letter-sink",
            "Worker dispatch dead-letter sink is not registered.");

        var status = issues.Count == 0
            ? AzureWorkerDispatcherReadinessStatus.Ready
            : AzureWorkerDispatcherReadinessStatus.NotReady;

        return Task.FromResult(
            new AzureWorkerDispatcherReadinessReport
            {
                Status = status,
                Issues = issues,
                EvaluatedAtUtc = DateTimeOffset.UtcNow
            });
    }

    private static void AddMissingIssueIfRequired(
        ICollection<AzureWorkerDispatcherReadinessIssue> issues,
        bool required,
        object? service,
        string component,
        string message)
    {
        if (!required || service is not null)
        {
            return;
        }

        issues.Add(
            new AzureWorkerDispatcherReadinessIssue
            {
                Code = "dispatcher.component.missing",
                Component = component,
                Message = message
            });
    }
}
