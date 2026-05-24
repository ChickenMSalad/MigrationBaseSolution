using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MigrationBase.Core.Cloud.Azure.ConnectorExecution;
using MigrationBase.Core.Cloud.Azure.FailureRuntime;
using MigrationBase.Core.Cloud.Azure.ManifestExecution;

namespace MigrationBase.Core.Cloud.Azure.EndToEndValidation;

public sealed class AzureEndToEndDryRunHarness : IAzureEndToEndDryRunHarness
{
    private readonly IAzureManifestExecutionPlanBuilder manifestPlanBuilder;
    private readonly IAzureManifestExecutionContextFactory contextFactory;
    private readonly IAzureManifestExecutionBatchRunner batchRunner;
    private readonly IAzureConnectorExecutor connectorExecutor;
    private readonly IAzureFailureClassifier failureClassifier;

    public AzureEndToEndDryRunHarness(
        IAzureManifestExecutionPlanBuilder manifestPlanBuilder,
        IAzureManifestExecutionContextFactory contextFactory,
        IAzureManifestExecutionBatchRunner batchRunner,
        IAzureConnectorExecutor connectorExecutor,
        IAzureFailureClassifier failureClassifier)
    {
        this.manifestPlanBuilder = manifestPlanBuilder ?? throw new ArgumentNullException(nameof(manifestPlanBuilder));
        this.contextFactory = contextFactory ?? throw new ArgumentNullException(nameof(contextFactory));
        this.batchRunner = batchRunner ?? throw new ArgumentNullException(nameof(batchRunner));
        this.connectorExecutor = connectorExecutor ?? throw new ArgumentNullException(nameof(connectorExecutor));
        this.failureClassifier = failureClassifier ?? throw new ArgumentNullException(nameof(failureClassifier));
    }

    public async Task<AzureEndToEndDryRunResult> RunAsync(
        AzureEndToEndDryRunRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.Scenario);

        var steps = new List<AzureEndToEndDryRunStepResult>();

        var plan = manifestPlanBuilder.Build(
            new AzureManifestExecutionPlanRequest
            {
                Scope = new AzureManifestExecutionScope
                {
                    RunId = request.RunId,
                    ManifestId = request.ManifestId,
                    Mode = AzureManifestExecutionMode.DryRun
                }
            });

        steps.Add(Passed("manifest-plan", "Manifest execution plan", "Dry-run manifest plan created."));

        var context = contextFactory.Create(
            new AzureManifestExecutionContextRequest
            {
                Plan = plan,
                InitialStatus = AzureManifestExecutionStatus.Running,
                RequestedBy = "end-to-end-validation"
            });

        steps.Add(Passed("manifest-context", "Manifest execution context", "Dry-run manifest context created."));

        var items = CreateSyntheticItems(request.ManifestId, request.SyntheticItemCount);
        var batch = new AzureManifestExecutionBatch
        {
            BatchId = Guid.NewGuid().ToString("n"),
            ExecutionId = context.ExecutionId,
            Items = items
        };

        var batchResult = await batchRunner.RunBatchAsync(
            new AzureManifestExecutionBatchRunRequest
            {
                Context = context,
                Batch = batch,
                AttemptNumber = 1,
                ContinueOnItemFailure = true
            },
            cancellationToken).ConfigureAwait(false);

        steps.Add(
            batchResult.HasFailures
                ? Failed("manifest-batch", "Manifest batch execution", "Dry-run manifest batch produced failures.")
                : Passed("manifest-batch", "Manifest batch execution", "Dry-run manifest batch completed."));

        var connectorResult = await connectorExecutor.ExecuteAsync(
            new AzureConnectorExecutionRequest
            {
                ExecutionId = context.ExecutionId,
                RunId = request.RunId,
                ManifestId = request.ManifestId,
                ItemId = "dry-run-connector-probe",
                SourceIdentifier = "dry-run-source",
                Mode = AzureConnectorExecutionMode.DryRun,
                Direction = AzureConnectorExecutionDirection.SourceRead
            },
            cancellationToken).ConfigureAwait(false);

        steps.Add(
            connectorResult.Status == AzureConnectorExecutionStatus.Failed
                ? Failed("connector-probe", "Connector execution probe", "Dry-run connector probe failed.")
                : Passed("connector-probe", "Connector execution probe", "Dry-run connector probe completed."));

        if (request.IncludeFailureRuntimeProbe)
        {
            var classification = failureClassifier.Classify(
                new AzureFailureSignal
                {
                    SignalId = Guid.NewGuid().ToString("n"),
                    Source = "end-to-end-validation",
                    RunId = request.RunId,
                    ManifestId = request.ManifestId,
                    WorkItemId = "dry-run-failure-probe",
                    ErrorCode = "timeout",
                    Message = "Synthetic timeout probe.",
                    AttemptNumber = 1
                });

            steps.Add(
                classification.RetryRecommended
                    ? Passed("failure-runtime-probe", "Failure runtime probe", "Synthetic transient failure was classified as retryable.")
                    : Failed("failure-runtime-probe", "Failure runtime probe", "Synthetic transient failure was not classified as retryable."));
        }

        return new AzureEndToEndDryRunResult
        {
            ScenarioId = request.Scenario.ScenarioId,
            CompletedAtUtc = DateTimeOffset.UtcNow,
            Steps = steps
        };
    }

    private static IReadOnlyList<AzureManifestExecutionItem> CreateSyntheticItems(
        string manifestId,
        int count)
    {
        var itemCount = Math.Max(1, count);
        var items = new List<AzureManifestExecutionItem>(itemCount);

        for (var index = 0; index < itemCount; index++)
        {
            items.Add(
                new AzureManifestExecutionItem
                {
                    ItemId = $"dry-run-item-{index + 1}",
                    ManifestId = manifestId,
                    SourceIdentifier = $"source-{index + 1}"
                });
        }

        return items;
    }

    private static AzureEndToEndDryRunStepResult Passed(
        string stepId,
        string name,
        string message)
    {
        return new AzureEndToEndDryRunStepResult
        {
            StepId = stepId,
            Name = name,
            Status = AzureEndToEndDryRunStepStatus.Passed,
            Message = message,
            CompletedAtUtc = DateTimeOffset.UtcNow
        };
    }

    private static AzureEndToEndDryRunStepResult Failed(
        string stepId,
        string name,
        string message)
    {
        return new AzureEndToEndDryRunStepResult
        {
            StepId = stepId,
            Name = name,
            Status = AzureEndToEndDryRunStepStatus.Failed,
            Message = message,
            CompletedAtUtc = DateTimeOffset.UtcNow
        };
    }
}
