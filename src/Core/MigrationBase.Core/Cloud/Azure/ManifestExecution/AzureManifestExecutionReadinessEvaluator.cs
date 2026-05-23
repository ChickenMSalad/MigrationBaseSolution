using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MigrationBase.Core.Cloud.Azure.ManifestExecution;

public sealed class AzureManifestExecutionReadinessEvaluator :
    IAzureManifestExecutionReadinessEvaluator
{
    private readonly IAzureManifestExecutionPlanBuilder? planBuilder;
    private readonly IAzureManifestExecutionContextFactory? contextFactory;
    private readonly IAzureManifestExecutionBatchProvider? batchProvider;
    private readonly IAzureManifestExecutionBatchRunner? batchRunner;
    private readonly IAzureManifestExecutionCheckpointStore? checkpointStore;
    private readonly IAzureManifestExecutionCompletionSink? completionSink;

    public AzureManifestExecutionReadinessEvaluator(
        IAzureManifestExecutionPlanBuilder? planBuilder = null,
        IAzureManifestExecutionContextFactory? contextFactory = null,
        IAzureManifestExecutionBatchProvider? batchProvider = null,
        IAzureManifestExecutionBatchRunner? batchRunner = null,
        IAzureManifestExecutionCheckpointStore? checkpointStore = null,
        IAzureManifestExecutionCompletionSink? completionSink = null)
    {
        this.planBuilder = planBuilder;
        this.contextFactory = contextFactory;
        this.batchProvider = batchProvider;
        this.batchRunner = batchRunner;
        this.checkpointStore = checkpointStore;
        this.completionSink = completionSink;
    }

    public Task<AzureManifestExecutionReadinessReport> EvaluateAsync(
        AzureManifestExecutionReadinessRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();

        var issues = new List<AzureManifestExecutionReadinessIssue>();

        AddMissingIssueIfRequired(
            issues,
            request.RequirePlanBuilder,
            planBuilder,
            "plan-builder",
            "Manifest execution plan builder is not registered.");

        AddMissingIssueIfRequired(
            issues,
            request.RequireContextFactory,
            contextFactory,
            "context-factory",
            "Manifest execution context factory is not registered.");

        AddMissingIssueIfRequired(
            issues,
            request.RequireBatchProvider,
            batchProvider,
            "batch-provider",
            "Manifest execution batch provider is not registered.");

        AddMissingIssueIfRequired(
            issues,
            request.RequireBatchRunner,
            batchRunner,
            "batch-runner",
            "Manifest execution batch runner is not registered.");

        AddMissingIssueIfRequired(
            issues,
            request.RequireCheckpointStore,
            checkpointStore,
            "checkpoint-store",
            "Manifest execution checkpoint store is not registered.");

        AddMissingIssueIfRequired(
            issues,
            request.RequireCompletionSink,
            completionSink,
            "completion-sink",
            "Manifest execution completion sink is not registered.");

        var status = issues.Count == 0
            ? AzureManifestExecutionReadinessStatus.Ready
            : AzureManifestExecutionReadinessStatus.NotReady;

        return Task.FromResult(
            new AzureManifestExecutionReadinessReport
            {
                Status = status,
                EvaluatedAtUtc = DateTimeOffset.UtcNow,
                Issues = issues
            });
    }

    private static void AddMissingIssueIfRequired(
        ICollection<AzureManifestExecutionReadinessIssue> issues,
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
            new AzureManifestExecutionReadinessIssue
            {
                Code = "manifest.execution.component.missing",
                Component = component,
                Message = message
            });
    }
}
